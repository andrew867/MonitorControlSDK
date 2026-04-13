/*
 * monitor_knobs_sdcp.ino
 *
 * ESP32: native SDCP/TCP VMC (no HTTP gateway). Multi-mode calibration / control panel:
 *
 *   MODE_PICTURE  — ADC: brightness + contrast (2 pots; third ADC ignored)
 *   MODE_RGB_GAIN — ADC: RGAIN, GGAIN, BGAIN (VMC command strings; model-dependent range)
 *   MODE_GRADE    — ADC: APERTURE (0–6), CHROMA + PHASE (0–100) per PVM-740-style VMC table
 *
 * Buttons (active LOW, INPUT_PULLUP):
 *   MODE   — short press: cycle mode (picture → RGB → grade → …)
 *   CAL    — short press: toggle FLATFIELDPATTERN ON/OFF (flat-field for calibration)
 *            long press (~1.5 s): STATset WBSEL USER then FLATFIELDPATTERN ON (green-field WB prep)
 *   POWER  — each press: alternate POWERSAVING OFF / ON (standby-ish; chassis-specific)
 *
 * Serial 115200: help | mode pic|rgb|grade | cap bmin… | cap rmin… | flat on|off
 *
 * NVS "kbcal": b0/b1/c0/c1 + r0/r1/g0/g1/b0/b1 + a0/a1/ch0/ch1/ph0/ph1 for per-mode ADC endpoints.
 */

#if !defined(ESP32)
#error "This native SDCP example targets ESP32 (WiFi + WiFiClient)."
#endif

#include <WiFi.h>
#include <WiFiClient.h>
#include <Preferences.h>
#include <string.h>
#include <strings.h>

static const char* WIFI_SSID = "your-ssid";
static const char* WIFI_PASSWORD = "your-password";

static const char* MONITOR_HOST = "192.168.1.10";
static const uint16_t SDCP_PORT = 53484;

// --- ADC (ESP32: prefer ADC1 pins with WiFi) ---
static const int PIN_ADC_A = 34;  // Bright / R gain / Aperture
static const int PIN_ADC_B = 35;  // Contrast / G gain / Chroma
static const int PIN_ADC_C = 32;  // (unused in picture mode) B gain / Phase

static const int ADC_FULL_SCALE = 4095;

// --- Buttons: connect to GND when pressed, use INPUT_PULLUP ---
static const int PIN_BTN_MODE = 25;
static const int PIN_BTN_CAL = 26;
static const int PIN_BTN_POWER = 27;

static const int HYSTERESIS = 6;
static const unsigned long MAX_INTERVAL_MS = 800;
static const unsigned long CAL_LONG_MS = 1500;
static const unsigned long BTN_DEBOUNCE_MS = 40;

static const size_t kSdcpMax = 973;
static const size_t kSdcpHdr = 13;

enum RunMode : uint8_t { MODE_PICTURE = 0, MODE_RGB_GAIN = 1, MODE_GRADE = 2, MODE_COUNT = 3 };

static RunMode gMode = MODE_PICTURE;
static Preferences gPrefs;

// Picture mode VMC 0..1023 (tune per chassis)
static const int VMC_PIC_LO = 0;
static const int VMC_PIC_HI = 1023;

// RGB gains (legacy tools often use 0..1023; validate on device)
static const int VMC_RGB_LO = 0;
static const int VMC_RGB_HI = 1023;

// PVM-740 manual style ranges for grading mode
static const int VMC_APERTURE_LO = 0;
static const int VMC_APERTURE_HI = 6;
static const int VMC_CHROMA_LO = 0;
static const int VMC_CHROMA_HI = 100;
static const int VMC_PHASE_LO = 0;
static const int VMC_PHASE_HI = 100;

// Calibration NVS
static int gB0, gB1, gC0, gC1;
static int gR0, gR1, gG0, gG1, gBl0, gBl1;
static int gA0, gA1, gCh0, gCh1, gPh0, gPh1;

static bool gFlatOn = false;
static bool gPowerStandby = false;  // when true, last sent was POWERSAVING ON

static int lastA = -1, lastB = -1, lastC = -1;
static unsigned long lastMs = 0;

static void clampPair(int& lo, int& hi) {
  if (lo < 0) lo = 0;
  if (hi > ADC_FULL_SCALE) hi = ADC_FULL_SCALE;
  if (hi <= lo) hi = (lo + 1 > ADC_FULL_SCALE) ? ADC_FULL_SCALE : lo + 1;
}

static void loadCalibration() {
  gPrefs.begin("kbcal", true);
  gB0 = gPrefs.getInt("b0", 0);
  gB1 = gPrefs.getInt("b1", ADC_FULL_SCALE);
  gC0 = gPrefs.getInt("c0", 0);
  gC1 = gPrefs.getInt("c1", ADC_FULL_SCALE);
  gR0 = gPrefs.getInt("r0", 0);
  gR1 = gPrefs.getInt("r1", ADC_FULL_SCALE);
  gG0 = gPrefs.getInt("g0", 0);
  gG1 = gPrefs.getInt("g1", ADC_FULL_SCALE);
  gBl0 = gPrefs.getInt("bl0", 0);
  gBl1 = gPrefs.getInt("bl1", ADC_FULL_SCALE);
  gA0 = gPrefs.getInt("a0", 0);
  gA1 = gPrefs.getInt("a1", ADC_FULL_SCALE);
  gCh0 = gPrefs.getInt("ch0", 0);
  gCh1 = gPrefs.getInt("ch1", ADC_FULL_SCALE);
  gPh0 = gPrefs.getInt("ph0", 0);
  gPh1 = gPrefs.getInt("ph1", ADC_FULL_SCALE);
  gPrefs.end();
  clampPair(gB0, gB1);
  clampPair(gC0, gC1);
  clampPair(gR0, gR1);
  clampPair(gG0, gG1);
  clampPair(gBl0, gBl1);
  clampPair(gA0, gA1);
  clampPair(gCh0, gCh1);
  clampPair(gPh0, gPh1);
}

static void saveCalibration() {
  gPrefs.begin("kbcal", false);
  gPrefs.putInt("b0", gB0);
  gPrefs.putInt("b1", gB1);
  gPrefs.putInt("c0", gC0);
  gPrefs.putInt("c1", gC1);
  gPrefs.putInt("r0", gR0);
  gPrefs.putInt("r1", gR1);
  gPrefs.putInt("g0", gG0);
  gPrefs.putInt("g1", gG1);
  gPrefs.putInt("bl0", gBl0);
  gPrefs.putInt("bl1", gBl1);
  gPrefs.putInt("a0", gA0);
  gPrefs.putInt("a1", gA1);
  gPrefs.putInt("ch0", gCh0);
  gPrefs.putInt("ch1", gCh1);
  gPrefs.putInt("ph0", gPh0);
  gPrefs.putInt("ph1", gPh1);
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

static int mapAdc(int raw, int rawMin, int rawMax, int vMin, int vMax) {
  clampPair(rawMin, rawMax);
  if (raw < rawMin) raw = rawMin;
  if (raw > rawMax) raw = rawMax;
  long v = vMin + (long)(vMax - vMin) * (raw - rawMin) / (rawMax - rawMin);
  if (v < vMin) v = vMin;
  if (v > vMax) v = vMax;
  return (int)v;
}

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

// Full STATset tail after "STATset " e.g. "POWERSAVING ON", "FLATFIELDPATTERN ON"
static size_t buildVmcStatSetTail(uint8_t* wire, const char* tail) {
  char ascii[920];
  int n = snprintf(ascii, sizeof(ascii), "STATset %s", tail);
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
  if (c.write(sendWire, sendLen) != sendLen) {
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

static bool vmcStatSet2(const char* host, const char* token, int value, char* err, size_t errSz) {
  char valStr[16];
  snprintf(valStr, sizeof(valStr), "%d", value);
  uint8_t wire[kSdcpMax];
  size_t len = buildVmcPacket(wire, "STATset", token, valStr);
  return sdcpVmcTransaction(host, wire, len, err, errSz);
}

static bool vmcStatSetTail(const char* host, const char* tail, char* err, size_t errSz) {
  uint8_t wire[kSdcpMax];
  size_t len = buildVmcStatSetTail(wire, tail);
  return len > 0 && sdcpVmcTransaction(host, wire, len, err, errSz);
}

static void trimInPlace(char* s) {
  char* p = s;
  while (*p == ' ' || *p == '\r') p++;
  if (p != s) memmove(s, p, strlen(p) + 1);
  size_t L = strlen(s);
  while (L > 0 && (s[L - 1] == ' ' || s[L - 1] == '\r')) s[--L] = 0;
}

static const char* modeName(RunMode m) {
  switch (m) {
    case MODE_PICTURE:
      return "PICTURE";
    case MODE_RGB_GAIN:
      return "RGB_GAIN";
    case MODE_GRADE:
      return "GRADE";
    default:
      return "?";
  }
}

static void handleSerialLine(char* line) {
  trimInPlace(line);
  if (!line[0]) return;
  if (!strcasecmp(line, "help")) {
    Serial.println(F("mode pic|rgb|grade"));
    Serial.println(F("cap bmin bmax cmin cmax | cap rmin rmax gmin gmax blmin blmax"));
    Serial.println(F("cap amin amax chmin chmax phmin phmax  (grade mode ADC ranges)"));
    Serial.println(F("flat on | flat off"));
    Serial.println(F("cal show | cal reset"));
    return;
  }
  if (!strcasecmp(line, "mode pic")) {
    gMode = MODE_PICTURE;
    lastA = lastB = lastC = -1;
    Serial.printf("mode=%s\n", modeName(gMode));
    return;
  }
  if (!strcasecmp(line, "mode rgb")) {
    gMode = MODE_RGB_GAIN;
    lastA = lastB = lastC = -1;
    Serial.printf("mode=%s\n", modeName(gMode));
    return;
  }
  if (!strcasecmp(line, "mode grade")) {
    gMode = MODE_GRADE;
    lastA = lastB = lastC = -1;
    Serial.printf("mode=%s\n", modeName(gMode));
    return;
  }
  if (!strcasecmp(line, "flat on")) {
    char err[80];
    if (vmcStatSetTail(MONITOR_HOST, "FLATFIELDPATTERN ON", err, sizeof(err))) {
      gFlatOn = true;
      Serial.println(F("FLAT ON ok"));
    } else
      Serial.printf("FLAT ON fail: %s\n", err);
    return;
  }
  if (!strcasecmp(line, "flat off")) {
    char err[80];
    if (vmcStatSetTail(MONITOR_HOST, "FLATFIELDPATTERN OFF", err, sizeof(err))) {
      gFlatOn = false;
      Serial.println(F("FLAT OFF ok"));
    } else
      Serial.printf("FLAT OFF fail: %s\n", err);
    return;
  }
  if (!strcasecmp(line, "cal show")) {
    Serial.printf("mode=%s flat=%s\n", modeName(gMode), gFlatOn ? "ON" : "OFF");
    Serial.printf("B ADC %d..%d  C %d..%d\n", gB0, gB1, gC0, gC1);
    Serial.printf("R %d..%d G %d..%d B %d..%d\n", gR0, gR1, gG0, gG1, gBl0, gBl1);
    Serial.printf("Ap %d..%d Ch %d..%d Ph %d..%d\n", gA0, gA1, gCh0, gCh1, gPh0, gPh1);
    return;
  }
  if (!strcasecmp(line, "cal reset")) {
    gB0 = gC0 = gR0 = gG0 = gBl0 = gA0 = gCh0 = gPh0 = 0;
    gB1 = gC1 = gR1 = gG1 = gBl1 = gA1 = gCh1 = gPh1 = ADC_FULL_SCALE;
    saveCalibration();
    Serial.println(F("cal reset ok"));
    return;
  }

  if (!strcasecmp(line, "cap bmin")) {
    gB0 = averageAnalog(PIN_ADC_A);
    saveCalibration();
    Serial.printf("bmin=%d\n", gB0);
  } else if (!strcasecmp(line, "cap bmax")) {
    gB1 = averageAnalog(PIN_ADC_A);
    saveCalibration();
    Serial.printf("bmax=%d\n", gB1);
  } else if (!strcasecmp(line, "cap cmin")) {
    gC0 = averageAnalog(PIN_ADC_B);
    saveCalibration();
    Serial.printf("cmin=%d\n", gC0);
  } else if (!strcasecmp(line, "cap cmax")) {
    gC1 = averageAnalog(PIN_ADC_B);
    saveCalibration();
    Serial.printf("cmax=%d\n", gC1);
  } else if (!strcasecmp(line, "cap rmin")) {
    gR0 = averageAnalog(PIN_ADC_A);
    saveCalibration();
    Serial.printf("rmin=%d\n", gR0);
  } else if (!strcasecmp(line, "cap rmax")) {
    gR1 = averageAnalog(PIN_ADC_A);
    saveCalibration();
    Serial.printf("rmax=%d\n", gR1);
  } else if (!strcasecmp(line, "cap gmin")) {
    gG0 = averageAnalog(PIN_ADC_B);
    saveCalibration();
    Serial.printf("gmin=%d\n", gG0);
  } else if (!strcasecmp(line, "cap gmax")) {
    gG1 = averageAnalog(PIN_ADC_B);
    saveCalibration();
    Serial.printf("gmax=%d\n", gG1);
  } else if (!strcasecmp(line, "cap blmin")) {
    gBl0 = averageAnalog(PIN_ADC_C);
    saveCalibration();
    Serial.printf("blmin=%d\n", gBl0);
  } else if (!strcasecmp(line, "cap blmax")) {
    gBl1 = averageAnalog(PIN_ADC_C);
    saveCalibration();
    Serial.printf("blmax=%d\n", gBl1);
  } else if (!strcasecmp(line, "cap amin")) {
    gA0 = averageAnalog(PIN_ADC_A);
    saveCalibration();
    Serial.printf("amin=%d\n", gA0);
  } else if (!strcasecmp(line, "cap amax")) {
    gA1 = averageAnalog(PIN_ADC_A);
    saveCalibration();
    Serial.printf("amax=%d\n", gA1);
  } else if (!strcasecmp(line, "cap chmin")) {
    gCh0 = averageAnalog(PIN_ADC_B);
    saveCalibration();
    Serial.printf("chmin=%d\n", gCh0);
  } else if (!strcasecmp(line, "cap chmax")) {
    gCh1 = averageAnalog(PIN_ADC_B);
    saveCalibration();
    Serial.printf("chmax=%d\n", gCh1);
  } else if (!strcasecmp(line, "cap phmin")) {
    gPh0 = averageAnalog(PIN_ADC_C);
    saveCalibration();
    Serial.printf("phmin=%d\n", gPh0);
  } else if (!strcasecmp(line, "cap phmax")) {
    gPh1 = averageAnalog(PIN_ADC_C);
    saveCalibration();
    Serial.printf("phmax=%d\n", gPh1);
  } else
    Serial.println(F("? (help)"));
}

static void pollSerial() {
  static char buf[72];
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

// --- Debounced buttons (INPUT_PULLUP: LOW = pressed) ---
static void pollEdgeButton(int pin, bool& stableReleased, unsigned long& lastEdgeMs, void (*onPress)()) {
  bool released = digitalRead(pin) == HIGH;
  unsigned long now = millis();
  if (released != stableReleased) {
    if (now - lastEdgeMs >= BTN_DEBOUNCE_MS) {
      stableReleased = released;
      lastEdgeMs = now;
      if (!released && onPress) onPress();
    }
  }
}

static unsigned long gModeLastMs = 0;
static bool gModeReleased = true;
static unsigned long gPwrLastMs = 0;
static bool gPwrReleased = true;

static void onModePress() {
  gMode = (RunMode)(((int)gMode + 1) % (int)MODE_COUNT);
  lastA = lastB = lastC = -1;
  Serial.printf("mode=%s\n", modeName(gMode));
}

static void onPowerPress() {
  char err[96];
  bool next = !gPowerStandby;
  const char* tail = next ? "POWERSAVING ON" : "POWERSAVING OFF";
  if (vmcStatSetTail(MONITOR_HOST, tail, err, sizeof(err))) {
    gPowerStandby = next;
    Serial.printf("POWER %s OK\n", tail);
  } else
    Serial.printf("POWER fail: %s\n", err);
}

static unsigned long gCalLastMs = 0;
static bool gCalReleased = true;
static unsigned long gCalPressedAt = 0;
static bool gCalLongDone = false;

static void pollCalButton() {
  bool released = digitalRead(PIN_BTN_CAL) == HIGH;
  unsigned long now = millis();

  if (released != gCalReleased) {
    if (now - gCalLastMs >= BTN_DEBOUNCE_MS) {
      gCalReleased = released;
      gCalLastMs = now;
      if (!released) {
        gCalPressedAt = now;
        gCalLongDone = false;
      } else {
        if (!gCalLongDone && gCalPressedAt > 0 && (now - gCalPressedAt) < CAL_LONG_MS) {
          char err[96];
          bool nextFlat = !gFlatOn;
          const char* tail = nextFlat ? "FLATFIELDPATTERN ON" : "FLATFIELDPATTERN OFF";
          if (vmcStatSetTail(MONITOR_HOST, tail, err, sizeof(err))) {
            gFlatOn = nextFlat;
            Serial.printf("CAL short %s OK\n", tail);
          } else
            Serial.printf("CAL short fail: %s\n", err);
        }
        gCalPressedAt = 0;
      }
    }
  }

  if (!gCalReleased && !gCalLongDone && gCalPressedAt > 0 && (now - gCalPressedAt) >= CAL_LONG_MS) {
    char err[96];
    gCalLongDone = true;
    if (vmcStatSetTail(MONITOR_HOST, "WBSEL USER", err, sizeof(err)))
      Serial.println(F("CAL long: WBSEL USER OK"));
    else
      Serial.printf("CAL long WBSEL fail: %s\n", err);
    if (vmcStatSetTail(MONITOR_HOST, "FLATFIELDPATTERN ON", err, sizeof(err))) {
      gFlatOn = true;
      Serial.println(F("CAL long: FLAT ON OK"));
    } else
      Serial.printf("CAL long FLAT fail: %s\n", err);
  }
}

void setup() {
  Serial.begin(115200);
  delay(200);
  analogReadResolution(12);
  loadCalibration();
  pinMode(PIN_ADC_A, INPUT);
  pinMode(PIN_ADC_B, INPUT);
  pinMode(PIN_ADC_C, INPUT);
  pinMode(PIN_BTN_MODE, INPUT_PULLUP);
  pinMode(PIN_BTN_CAL, INPUT_PULLUP);
  pinMode(PIN_BTN_POWER, INPUT_PULLUP);
  gModeReleased = digitalRead(PIN_BTN_MODE) == HIGH;
  gPwrReleased = digitalRead(PIN_BTN_POWER) == HIGH;
  gCalReleased = digitalRead(PIN_BTN_CAL) == HIGH;
  gModeLastMs = gPwrLastMs = gCalLastMs = millis();

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while (WiFi.status() != WL_CONNECTED) {
    delay(400);
    Serial.print('.');
  }
  Serial.println();
  Serial.println(WiFi.localIP());
  Serial.println(F("Buttons: MODE cycle | CAL flat toggle / long=WBSEL+FLAT | POWER POWERSAVING"));
  Serial.println(F("Serial: help"));
}

void loop() {
  pollSerial();
  pollEdgeButton(PIN_BTN_MODE, gModeReleased, gModeLastMs, onModePress);
  pollEdgeButton(PIN_BTN_POWER, gPwrReleased, gPwrLastMs, onPowerPress);
  pollCalButton();

  int a = averageAnalog(PIN_ADC_A);
  int b = averageAnalog(PIN_ADC_B);
  int c = averageAnalog(PIN_ADC_C);
  unsigned long now = millis();

  int va = 0, vb = 0, vc = 0;
  switch (gMode) {
    case MODE_PICTURE:
      va = mapAdc(a, gB0, gB1, VMC_PIC_LO, VMC_PIC_HI);
      vb = mapAdc(b, gC0, gC1, VMC_PIC_LO, VMC_PIC_HI);
      break;
    case MODE_RGB_GAIN:
      va = mapAdc(a, gR0, gR1, VMC_RGB_LO, VMC_RGB_HI);
      vb = mapAdc(b, gG0, gG1, VMC_RGB_LO, VMC_RGB_HI);
      vc = mapAdc(c, gBl0, gBl1, VMC_RGB_LO, VMC_RGB_HI);
      break;
    case MODE_GRADE:
      va = mapAdc(a, gA0, gA1, VMC_APERTURE_LO, VMC_APERTURE_HI);
      vb = mapAdc(b, gCh0, gCh1, VMC_CHROMA_LO, VMC_CHROMA_HI);
      vc = mapAdc(c, gPh0, gPh1, VMC_PHASE_LO, VMC_PHASE_HI);
      break;
    default:
      break;
  }

  bool moved = (lastA < 0) || abs(va - lastA) >= HYSTERESIS || (lastB < 0) || abs(vb - lastB) >= HYSTERESIS;
  if (gMode != MODE_PICTURE) moved = moved || (lastC < 0) || abs(vc - lastC) >= HYSTERESIS;
  bool force = (now - lastMs >= MAX_INTERVAL_MS);

  if (moved || force) {
    char err[96];
    bool ok = true;
    switch (gMode) {
      case MODE_PICTURE:
        ok = vmcStatSet2(MONITOR_HOST, "BRIGHTNESS", va, err, sizeof(err));
        if (!ok) Serial.printf("BRIGHT: %s\n", err);
        ok = vmcStatSet2(MONITOR_HOST, "CONTRAST", vb, err, sizeof(err)) && ok;
        if (!ok) Serial.printf("CONT: %s\n", err);
        lastA = va;
        lastB = vb;
        break;
      case MODE_RGB_GAIN:
        ok = vmcStatSet2(MONITOR_HOST, "RGAIN", va, err, sizeof(err));
        if (!ok) Serial.printf("RGAIN: %s\n", err);
        ok = vmcStatSet2(MONITOR_HOST, "GGAIN", vb, err, sizeof(err)) && ok;
        if (!ok) Serial.printf("GGAIN: %s\n", err);
        ok = vmcStatSet2(MONITOR_HOST, "BGAIN", vc, err, sizeof(err)) && ok;
        if (!ok) Serial.printf("BGAIN: %s\n", err);
        lastA = va;
        lastB = vb;
        lastC = vc;
        break;
      case MODE_GRADE:
        ok = vmcStatSet2(MONITOR_HOST, "APERTURE", va, err, sizeof(err));
        if (!ok) Serial.printf("APERTURE: %s\n", err);
        ok = vmcStatSet2(MONITOR_HOST, "CHROMA", vb, err, sizeof(err)) && ok;
        if (!ok) Serial.printf("CHROMA: %s\n", err);
        ok = vmcStatSet2(MONITOR_HOST, "PHASE", vc, err, sizeof(err)) && ok;
        if (!ok) Serial.printf("PHASE: %s\n", err);
        lastA = va;
        lastB = vb;
        lastC = vc;
        break;
      default:
        break;
    }
    lastMs = now;
    if (gMode == MODE_PICTURE)
      Serial.printf("[%s] BRIGHT=%d CONT=%d\n", modeName(gMode), va, vb);
    else
      Serial.printf("[%s] A=%d B=%d C=%d\n", modeName(gMode), va, vb, vc);
  }
  delay(40);
}
