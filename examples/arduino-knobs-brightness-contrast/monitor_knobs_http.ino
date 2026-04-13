/*
 * monitor_knobs_http.ino
 *
 * Map two ADC inputs (pots) to VMC picture controls via MonitorControl.Web:
 *   STATset BRIGHTNESS <n>
 *   STATset CONTRAST <n>
 *
 * Targets: ESP32, ESP8266 (Arduino core).
 * Calibration: per-device ADC min/max stored in NVS (ESP32) or EEPROM (ESP8266).
 * Serial (115200): "help" — capture commands for endpoints.
 *
 * See README.md in this folder for wiring and gateway setup.
 */

#if !defined(ESP32) && !defined(ESP8266)
#error "This sketch targets ESP32 or ESP8266 (WiFi + HTTP). See README for classic Arduino options."
#endif

#if defined(ESP32)
#include <WiFi.h>
#include <HTTPClient.h>
#include <Preferences.h>
#elif defined(ESP8266)
#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>
#include <WiFiClient.h>
#include <EEPROM.h>
#endif
#include <string.h>
#include <strings.h>

/* ---- User configuration ---- */
static const char* WIFI_SSID = "your-ssid";
static const char* WIFI_PASSWORD = "your-password";

// Machine running: dotnet run --project src/MonitorControl.Web --urls http://0.0.0.0:5080
static const char* GATEWAY_HOST = "192.168.1.50";
static const uint16_t GATEWAY_PORT = 5080;

// Monitor SDCP IP (same as "host" in the bundled web UI)
static const char* MONITOR_HOST = "192.168.1.10";

// ADC pins
#if defined(ESP32)
static const int ADC_BRIGHT_PIN = 34;
static const int ADC_CONTRAST_PIN = 35;
#elif defined(ESP8266)
static const int ADC_BRIGHT_PIN = A0;
static const int ADC_CONTRAST_PIN = A0;
#endif

#if defined(ESP32)
static const int ADC_FULL_SCALE = 4095;
#elif defined(ESP8266)
static const int ADC_FULL_SCALE = 1023;
#endif

// VMC numeric range sent to the monitor (model-dependent)
static const int VMC_MIN = 0;
static const int VMC_MAX = 1023;

static const int HYSTERESIS = 6;
static const unsigned long MAX_INTERVAL_MS = 800;

/* ---- Calibration (ADC raw endpoints -> maps to full VMC range) ---- */
static int gAdcBrightMin = 0;
static int gAdcBrightMax = ADC_FULL_SCALE;
static int gAdcContrastMin = 0;
static int gAdcContrastMax = ADC_FULL_SCALE;

#if defined(ESP32)
static Preferences gPrefs;
#elif defined(ESP8266)
static const int EEPROM_SIZE = 32;
static const uint32_t EEPROM_MAGIC = 0x314B4243;  // 'CBK1'
struct CalBlob {
  uint32_t magic;
  int b0, b1, c0, c1;
};
#endif

static void loadCalibration() {
#if defined(ESP32)
  gPrefs.begin("kbcal", true);
  gAdcBrightMin = gPrefs.getInt("b0", 0);
  gAdcBrightMax = gPrefs.getInt("b1", ADC_FULL_SCALE);
  gAdcContrastMin = gPrefs.getInt("c0", 0);
  gAdcContrastMax = gPrefs.getInt("c1", ADC_FULL_SCALE);
  gPrefs.end();
#elif defined(ESP8266)
  CalBlob blob{};
  EEPROM.get(0, blob);
  if (blob.magic == EEPROM_MAGIC) {
    gAdcBrightMin = blob.b0;
    gAdcBrightMax = blob.b1;
    gAdcContrastMin = blob.c0;
    gAdcContrastMax = blob.c1;
  }
#endif
  clampCalBounds();
}

static void clampCalBounds() {
  auto clampPair = [](int& lo, int& hi) {
    if (lo < 0) lo = 0;
    if (hi > ADC_FULL_SCALE) hi = ADC_FULL_SCALE;
    if (hi <= lo) hi = (lo + 1 > ADC_FULL_SCALE) ? ADC_FULL_SCALE : lo + 1;
  };
  clampPair(gAdcBrightMin, gAdcBrightMax);
  clampPair(gAdcContrastMin, gAdcContrastMax);
}

static void saveCalibration() {
  clampCalBounds();
#if defined(ESP32)
  gPrefs.begin("kbcal", false);
  gPrefs.putInt("b0", gAdcBrightMin);
  gPrefs.putInt("b1", gAdcBrightMax);
  gPrefs.putInt("c0", gAdcContrastMin);
  gPrefs.putInt("c1", gAdcContrastMax);
  gPrefs.end();
#elif defined(ESP8266)
  CalBlob blob{EEPROM_MAGIC, gAdcBrightMin, gAdcBrightMax, gAdcContrastMin, gAdcContrastMax};
  EEPROM.put(0, blob);
  EEPROM.commit();
#endif
}

static int averageAnalog(int pin) {
  long acc = 0;
  const int n = 8;
  for (int i = 0; i < n; i++) {
    acc += analogRead(pin);
    delay(1);
  }
  int raw = (int)(acc / n);
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

static int readMappedBright() {
  return mapAdcToVmc(averageAnalog(ADC_BRIGHT_PIN), gAdcBrightMin, gAdcBrightMax);
}

static int readMappedContrast() {
  return mapAdcToVmc(averageAnalog(ADC_CONTRAST_PIN), gAdcContrastMin, gAdcContrastMax);
}

static bool postVmcSet(const char* token, int value) {
  char body[192];
  snprintf(body, sizeof(body),
           "{\"host\":\"%s\",\"args\":[\"%s\",\"%d\"]}",
           MONITOR_HOST, token, value);

  char url[96];
  snprintf(url, sizeof(url), "http://%s:%u/api/vmc/set", GATEWAY_HOST, GATEWAY_PORT);

#if defined(ESP32)
  HTTPClient http;
  http.begin(url);
  http.addHeader("Content-Type", "application/json");
  int code = http.POST((uint8_t*)body, strlen(body));
  http.end();
  return code >= 200 && code < 300;
#elif defined(ESP8266)
  WiFiClient client;
  HTTPClient http;
  if (!http.begin(client, GATEWAY_HOST, GATEWAY_PORT, "/api/vmc/set")) {
    return false;
  }
  http.addHeader("Content-Type", "application/json");
  int code = http.POST(body);
  http.end();
  return code >= 200 && code < 300;
#endif
}

static void trimInPlace(char* s) {
  char* p = s;
  while (*p == ' ' || *p == '\r') p++;
  if (p != s) memmove(s, p, strlen(p) + 1);
  size_t L = strlen(s);
  while (L > 0 && (s[L - 1] == ' ' || s[L - 1] == '\r')) {
    s[--L] = 0;
  }
}

static void handleSerialLine(char* line) {
  trimInPlace(line);
  if (!line[0]) return;

  if (!strcasecmp(line, "help")) {
    Serial.println(F("Commands (set pots, then type):"));
    Serial.println(F("  cap bmin | cap bmax | cap cmin | cap cmax  — store current ADC as endpoint"));
    Serial.println(F("  cal show  — print ADC ranges"));
    Serial.println(F("  cal reset — factory ADC 0..ADC_FULL_SCALE"));
    return;
  }
  if (!strcasecmp(line, "cal show")) {
    Serial.printf("bright ADC %d..%d  contrast ADC %d..%d\n", gAdcBrightMin, gAdcBrightMax, gAdcContrastMin,
                  gAdcContrastMax);
    return;
  }
  if (!strcasecmp(line, "cal reset")) {
    gAdcBrightMin = 0;
    gAdcBrightMax = ADC_FULL_SCALE;
    gAdcContrastMin = 0;
    gAdcContrastMax = ADC_FULL_SCALE;
    saveCalibration();
    Serial.println(F("Calibration reset."));
    return;
  }

  int v = averageAnalog((!strcasecmp(line, "cap cmin") || !strcasecmp(line, "cap cmax")) ? ADC_CONTRAST_PIN : ADC_BRIGHT_PIN);

  if (!strcasecmp(line, "cap bmin")) {
    gAdcBrightMin = v;
    saveCalibration();
    Serial.printf("bmin=%d saved\n", v);
  } else if (!strcasecmp(line, "cap bmax")) {
    gAdcBrightMax = v;
    saveCalibration();
    Serial.printf("bmax=%d saved\n", v);
  } else if (!strcasecmp(line, "cap cmin")) {
    gAdcContrastMin = v;
    saveCalibration();
    Serial.printf("cmin=%d saved\n", v);
  } else if (!strcasecmp(line, "cap cmax")) {
    gAdcContrastMax = v;
    saveCalibration();
    Serial.printf("cmax=%d saved\n", v);
  } else {
    Serial.println(F("Unknown. Type: help"));
  }
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
    } else if (len + 1 < sizeof(buf)) {
      buf[len++] = ch;
    }
  }
}

static int lastBright = -1;
static int lastContrast = -1;
static unsigned long lastPostMs = 0;

void setup() {
  Serial.begin(115200);
  delay(200);

#if defined(ESP32)
  analogReadResolution(12);
#endif

#if defined(ESP8266)
  EEPROM.begin(EEPROM_SIZE);
#endif

  loadCalibration();

  pinMode(ADC_BRIGHT_PIN, INPUT);
  pinMode(ADC_CONTRAST_PIN, INPUT);

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  Serial.print(F("WiFi "));
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(F("."));
  }
  Serial.println();
  Serial.print(F("IP: "));
  Serial.println(WiFi.localIP());
  Serial.println(F("Serial: type 'help' for calibration."));
}

void loop() {
  pollSerial();

  int b = readMappedBright();
  int c = readMappedContrast();
  unsigned long now = millis();

  bool brightMoved = (lastBright < 0) || (abs(b - lastBright) >= HYSTERESIS);
  bool contrastMoved = (lastContrast < 0) || (abs(c - lastContrast) >= HYSTERESIS);
  bool force = (now - lastPostMs >= MAX_INTERVAL_MS);

  if (brightMoved || contrastMoved || force) {
    bool okB = true;
    bool okC = true;
    if (brightMoved || force) {
      okB = postVmcSet("BRIGHTNESS", b);
      if (okB) lastBright = b;
    }
    if (contrastMoved || force) {
      okC = postVmcSet("CONTRAST", c);
      if (okC) lastContrast = c;
    }
    lastPostMs = now;

    Serial.print(F("BRIGHTNESS="));
    Serial.print(b);
    Serial.print(okB ? F(" OK ") : F(" FAIL "));
    Serial.print(F("CONTRAST="));
    Serial.print(c);
    Serial.println(okC ? F(" OK") : F(" FAIL"));
  }

  delay(40);
}
