/*
 * monitor_knobs_sdcp.ino
 *
 * ESP32: two ADC pots -> map -> native SDCP/TCP VMC STATset (no HTTP gateway).
 * Matches Sony.MonitorControl wire format: SDCP v3, item 0xB000, P2P header, ASCII payload.
 *
 * Docs: docs/reference/sdcp-framing-and-items.md
 * Calibration: same NVS keys as monitor_knobs_http.ino ("kbcal" namespace).
 */

#if !defined(ESP32)
#error "This native SDCP example targets ESP32 (WiFi + WiFiClient). See monitor_knobs_http.ino for ESP8266 via HTTP."
#endif

#include <WiFi.h>
#include <WiFiClient.h>
#include <Preferences.h>
#include <string.h>
#include <strings.h>

static const char* WIFI_SSID = "your-ssid";
static const char* WIFI_PASSWORD = "your-password";

// Monitor listens on SDCP TCP (default 53484)
static const char* MONITOR_HOST = "192.168.1.10";
static const uint16_t SDCP_PORT = 53484;

static const int ADC_BRIGHT_PIN = 34;
static const int ADC_CONTRAST_PIN = 35;
static const int ADC_FULL_SCALE = 4095;

static const int VMC_MIN = 0;
static const int VMC_MAX = 1023;
static const int HYSTERESIS = 6;
static const unsigned long MAX_INTERVAL_MS = 800;

static const size_t kSdcpMax = 973;
static const size_t kSdcpHdr = 13;

static int gAdcBrightMin = 0;
static int gAdcBrightMax = ADC_FULL_SCALE;
static int gAdcContrastMin = 0;
static int gAdcContrastMax = ADC_FULL_SCALE;
static Preferences gPrefs;

static void clampCalBounds() {
  auto clampPair = [](int& lo, int& hi) {
    if (lo < 0) lo = 0;
    if (hi > ADC_FULL_SCALE) hi = ADC_FULL_SCALE;
    if (hi <= lo) hi = (lo + 1 > ADC_FULL_SCALE) ? ADC_FULL_SCALE : lo + 1;
  };
  clampPair(gAdcBrightMin, gAdcBrightMax);
  clampPair(gAdcContrastMin, gAdcContrastMax);
}

static void loadCalibration() {
  gPrefs.begin("kbcal", true);
  gAdcBrightMin = gPrefs.getInt("b0", 0);
  gAdcBrightMax = gPrefs.getInt("b1", ADC_FULL_SCALE);
  gAdcContrastMin = gPrefs.getInt("c0", 0);
  gAdcContrastMax = gPrefs.getInt("c1", ADC_FULL_SCALE);
  gPrefs.end();
  clampCalBounds();
}

static void saveCalibration() {
  clampCalBounds();
  gPrefs.begin("kbcal", false);
  gPrefs.putInt("b0", gAdcBrightMin);
  gPrefs.putInt("b1", gAdcBrightMax);
  gPrefs.putInt("c0", gAdcContrastMin);
  gPrefs.putInt("c1", gAdcContrastMax);
  gPrefs.end();
}

static int averageAnalog(int pin) {
  long acc = 0;
  for (int i = 0; i < 8; i++) {
    acc += analogRead(pin);
    delay(1);
  }
  int raw = (int)(acc / 8);
  if (raw < 0) raw = 0;
  if (raw > ADC_FULL_SCALE) raw = ADC_FULL_SCALE;
  return raw;
}

static int mapAdcToVmc(int raw, int rawMin, int rawMax) {
  if (rawMax <= rawMin) rawMax = rawMin + 1;
  if (raw < rawMin) raw = rawMin;
  if (raw > rawMax) raw = rawMax;
  long v = VMC_MIN + (long)(VMC_MAX - VMC_MIN) * (raw - rawMin) / (rawMax - rawMin);
  if (v < VMC_MIN) v = VMC_MIN;
  if (v > VMC_MAX) v = VMC_MAX;
  return (int)v;
}

// Build SDCP v3 VMC frame (P2P, item 0xB000). Payload = ASCII without trailing NUL.
static size_t buildVmcPacket(uint8_t* wire, const char* category, const char* arg1, const char* arg2) {
  char ascii[920];
  int n;
  if (arg2 && arg2[0])
    n = snprintf(ascii, sizeof(ascii), "%s %s %s", category, arg1, arg2);
  else if (arg1 && arg1[0])
    n = snprintf(ascii, sizeof(ascii), "%s %s", category, arg1);
  else
    n = snprintf(ascii, sizeof(ascii), "%s", category);
  if (n <= 0 || (size_t)n >= sizeof(ascii)) return 0;

  memset(wire, 0, kSdcpMax);
  wire[0] = 3;
  wire[1] = 11;
  wire[2] = 'S';
  wire[3] = 'O';
  wire[4] = 'N';
  wire[5] = 'Y';
  wire[6] = 0;
  wire[7] = 0;
  wire[8] = 0;
  wire[9] = 0xB0;
  wire[10] = 0x00;
  uint16_t dlen = (uint16_t)strlen(ascii);
  wire[11] = (uint8_t)(dlen >> 8);
  wire[12] = (uint8_t)(dlen & 0xFF);
  memcpy(wire + kSdcpHdr, ascii, dlen);
  return kSdcpHdr + dlen;
}

// Returns true if wire[8] is OK (1) after full 973-byte read (matches SdcpConnection.receivePacket).
static bool sdcpVmcTransaction(const char* host, const uint8_t* sendWire, size_t sendLen, char* err, size_t errSz) {
  if (sendLen == 0 || sendLen > kSdcpMax) {
    snprintf(err, errSz, "bad sendLen");
    return false;
  }

  WiFiClient c;
  if (!c.connect(host, SDCP_PORT)) {
    snprintf(err, errSz, "tcp connect failed");
    return false;
  }

  c.setTimeout(12);
  size_t w = c.write(sendWire, sendLen);
  if (w != sendLen) {
    snprintf(err, errSz, "short write");
    c.stop();
    return false;
  }

  uint8_t rx[kSdcpMax];
  size_t got = 0;
  unsigned long start = millis();
  while (got < kSdcpMax && c.connected() && (millis() - start < 15000)) {
    int n = c.read(rx + got, kSdcpMax - got);
    if (n < 0) break;
    if (n == 0) delay(1);
    else
      got += (size_t)n;
  }
  c.stop();

  if (got < kSdcpHdr) {
    snprintf(err, errSz, "short read %u", (unsigned)got);
    return false;
  }
  if (rx[8] != 1) {
    snprintf(err, errSz, "SDCP NAK (byte8=%u)", (unsigned)rx[8]);
    return false;
  }
  return true;
}

static bool vmcStatSet(const char* host, const char* token, int value, char* err, size_t errSz) {
  char valStr[16];
  snprintf(valStr, sizeof(valStr), "%d", value);
  uint8_t wire[kSdcpMax];
  size_t len = buildVmcPacket(wire, "STATset", token, valStr);
  return sdcpVmcTransaction(host, wire, len, err, errSz);
}

static void trimInPlace(char* s) {
  char* p = s;
  while (*p == ' ' || *p == '\r') p++;
  if (p != s) memmove(s, p, strlen(p) + 1);
  size_t L = strlen(s);
  while (L > 0 && (s[L - 1] == ' ' || s[L - 1] == '\r')) s[--L] = 0;
}

static void handleSerialLine(char* line) {
  trimInPlace(line);
  if (!line[0]) return;
  if (!strcasecmp(line, "help")) {
    Serial.println(F("cap bmin|bmax|cmin|cmax  cal show|reset"));
    return;
  }
  if (!strcasecmp(line, "cal show")) {
    Serial.printf("bright ADC %d..%d  contrast %d..%d\n", gAdcBrightMin, gAdcBrightMax, gAdcContrastMin, gAdcContrastMax);
    return;
  }
  if (!strcasecmp(line, "cal reset")) {
    gAdcBrightMin = 0;
    gAdcBrightMax = ADC_FULL_SCALE;
    gAdcContrastMin = 0;
    gAdcContrastMax = ADC_FULL_SCALE;
    saveCalibration();
    Serial.println(F("reset ok"));
    return;
  }
  int pin = (!strcasecmp(line, "cap cmin") || !strcasecmp(line, "cap cmax")) ? ADC_CONTRAST_PIN : ADC_BRIGHT_PIN;
  int v = averageAnalog(pin);
  if (!strcasecmp(line, "cap bmin")) {
    gAdcBrightMin = v;
    saveCalibration();
    Serial.printf("bmin=%d\n", v);
  } else if (!strcasecmp(line, "cap bmax")) {
    gAdcBrightMax = v;
    saveCalibration();
    Serial.printf("bmax=%d\n", v);
  } else if (!strcasecmp(line, "cap cmin")) {
    gAdcContrastMin = v;
    saveCalibration();
    Serial.printf("cmin=%d\n", v);
  } else if (!strcasecmp(line, "cap cmax")) {
    gAdcContrastMax = v;
    saveCalibration();
    Serial.printf("cmax=%d\n", v);
  } else
    Serial.println(F("?"));
}

static void pollSerial() {
  static char buf[64];
  static size_t len = 0;
  while (Serial.available()) {
    char ch = (char)Serial.read();
    if (ch == '\n' || ch == '\r') {
      if (len > 0) {
        buf[len] = 0;
        handleSerialLine(buf);
        len = 0;
      }
    } else if (len + 1 < sizeof(buf))
      buf[len++] = ch;
  }
}

static int lastB = -1, lastC = -1;
static unsigned long lastMs = 0;

void setup() {
  Serial.begin(115200);
  delay(200);
  analogReadResolution(12);
  loadCalibration();
  pinMode(ADC_BRIGHT_PIN, INPUT);
  pinMode(ADC_CONTRAST_PIN, INPUT);

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while (WiFi.status() != WL_CONNECTED) {
    delay(400);
    Serial.print('.');
  }
  Serial.println();
  Serial.println(WiFi.localIP());
}

void loop() {
  pollSerial();
  int b = mapAdcToVmc(averageAnalog(ADC_BRIGHT_PIN), gAdcBrightMin, gAdcBrightMax);
  int c = mapAdcToVmc(averageAnalog(ADC_CONTRAST_PIN), gAdcContrastMin, gAdcContrastMax);
  unsigned long now = millis();
  bool moved = (lastB < 0) || (abs(b - lastB) >= HYSTERESIS) || (lastC < 0) || (abs(c - lastC) >= HYSTERESIS);
  bool force = (now - lastMs >= MAX_INTERVAL_MS);

  if (moved || force) {
    char err[80];
    bool okB = true, okC = true;
    if (moved || force) {
      okB = vmcStatSet(MONITOR_HOST, "BRIGHTNESS", b, err, sizeof(err));
      if (!okB) Serial.printf("BRIGHT fail: %s\n", err);
      else lastB = b;
    }
    if (moved || force) {
      okC = vmcStatSet(MONITOR_HOST, "CONTRAST", c, err, sizeof(err));
      if (!okC) Serial.printf("CONT fail: %s\n", err);
      else lastC = c;
    }
    lastMs = now;
    Serial.printf("B=%d %s C=%d %s\n", b, okB ? "OK" : "X", c, okC ? "OK" : "X");
  }
  delay(40);
}
