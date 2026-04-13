/*
 * wifi_sdap_web.ino — WiFi + SDAP (UDP 53862) + HTML5 config portal
 * Merged by Arduino IDE with monitor_knobs_sdcp.ino (same folder).
 * SDAP layout matches MonitorControl.Protocol.SdapAdvertisementPacket (DA header, SONY, IP @50-53).
 */

#include <WebServer.h>
#include <WiFiUdp.h>
#include <DNSServer.h>
#include <Preferences.h>
#include "config_portal.h"

static constexpr uint16_t kSdapPort = 53862;
static constexpr uint16_t kCfgWebStaPort = 8080;

static char gWifiSsid[33];
static char gWifiPass[65];
static char gMonitorHost[48];
static bool gHasWifiPass = false;
static bool gPortalApMode = false;
static WebServer gWeb(80);
static DNSServer gDns;
static WiFiUDP gSdapUdp;
static bool gSdapBegun = false;
static bool gWebStarted = false;
static char gApSsidBuf[40];

static void cfgLoad() {
  gWifiSsid[0] = gWifiPass[0] = gMonitorHost[0] = 0;
  gHasWifiPass = false;
  Preferences p;
  if (!p.begin("mcfg", true)) return;
  String s = p.getString("ssid", "");
  String pw = p.getString("pass", "");
  String mh = p.getString("mhost", "");
  strncpy(gWifiSsid, s.c_str(), sizeof(gWifiSsid) - 1);
  gWifiSsid[sizeof(gWifiSsid) - 1] = 0;
  strncpy(gWifiPass, pw.c_str(), sizeof(gWifiPass) - 1);
  gWifiPass[sizeof(gWifiPass) - 1] = 0;
  strncpy(gMonitorHost, mh.c_str(), sizeof(gMonitorHost) - 1);
  gMonitorHost[sizeof(gMonitorHost) - 1] = 0;
  gHasWifiPass = pw.length() > 0;
  p.end();
}

static void cfgSaveWifi(const char* ssid, const char* pass, bool passProvided) {
  Preferences p;
  p.begin("mcfg", false);
  p.putString("ssid", ssid);
  if (passProvided && pass[0]) {
    p.putString("pass", pass);
    strncpy(gWifiPass, pass, sizeof(gWifiPass) - 1);
    gWifiPass[sizeof(gWifiPass) - 1] = 0;
    gHasWifiPass = true;
  }
  p.end();
  strncpy(gWifiSsid, ssid, sizeof(gWifiSsid) - 1);
  gWifiSsid[sizeof(gWifiSsid) - 1] = 0;
}

static void cfgSaveMonitor(const char* host) {
  Preferences p;
  p.begin("mcfg", false);
  p.putString("mhost", host);
  p.end();
  strncpy(gMonitorHost, host, sizeof(gMonitorHost) - 1);
  gMonitorHost[sizeof(gMonitorHost) - 1] = 0;
}

static void cfgErase() {
  Preferences p;
  p.begin("mcfg", false);
  p.clear();
  p.end();
}

static bool cfgWifiOk() { return gWifiSsid[0] != 0; }

static bool sdapDecode(const uint8_t* b, int len, char* ipOut, char* prodOut, size_t prodSz, char* serOut, size_t serSz) {
  if (len < 122) return false;
  if (b[0] != 'D' || b[1] != 'A') return false;
  if (b[4] != 'S' || b[5] != 'O' || b[6] != 'N' || b[7] != 'Y') return false;
  size_t pi = 0;
  for (int i = 8; i < 20 && pi + 1 < prodSz; i++) {
    if (b[i] == 0) break;
    prodOut[pi++] = (char)b[i];
  }
  prodOut[pi] = 0;
  uint32_t sn = ((uint32_t)b[20] << 24) | ((uint32_t)b[21] << 16) | ((uint32_t)b[22] << 8) | b[23];
  snprintf(serOut, serSz, "%lu", (unsigned long)sn);
  snprintf(ipOut, 18, "%u.%u.%u.%u", b[50], b[51], b[52], b[53]);
  return true;
}

static void jsonEscape(const char* in, char* out, size_t outSz) {
  size_t o = 0;
  for (size_t i = 0; in[i] && o + 2 < outSz; i++) {
    char c = in[i];
    if (c == '"' || c == '\\') {
      if (o + 3 >= outSz) break;
      out[o++] = '\\';
    }
    out[o++] = c;
  }
  out[o] = 0;
}

static void sdapEnsureSocket() {
  if (gSdapBegun) return;
  if (gPortalApMode) return;
  if (WiFi.status() != WL_CONNECTED) return;
  if (gSdapUdp.begin(kSdapPort)) gSdapBegun = true;
}

static void sdapStop() {
  if (gSdapBegun) {
    gSdapUdp.stop();
    gSdapBegun = false;
  }
}

static void handleApiStatus() {
  char json[512];
  char escSsid[80];
  jsonEscape(gWifiSsid, escSsid, sizeof(escSsid));
  const char* mode = gPortalApMode ? "ap_portal" : "sta";
  IPAddress ip = gPortalApMode ? WiFi.softAPIP() : WiFi.localIP();
  snprintf(json, sizeof(json),
           "{\"mode\":\"%s\",\"wifi\":\"%s\",\"ip\":\"%s\",\"staIp\":\"%s\",\"apIp\":\"%s\",\"apSsid\":\"%s\",\"monitor\":\"%s\","
           "\"storedSsid\":\"%s\",\"hasPass\":%s}",
           mode, WiFi.SSID().c_str(), ip.toString().c_str(), WiFi.localIP().toString().c_str(),
           WiFi.softAPIP().toString().c_str(), gApSsidBuf, gMonitorHost, escSsid, gHasWifiPass ? "true" : "false");
  gWeb.send(200, "application/json", json);
}

static void handleWifiScan() {
  int n = WiFi.scanNetworks();
  char buf[2048];
  size_t o = 0;
  o += snprintf(buf + o, sizeof(buf) - o, "{\"networks\":[");
  for (int i = 0; i < n && o < sizeof(buf) - 120; i++) {
    if (i) buf[o++] = ',';
    char esc[96];
    jsonEscape(WiFi.SSID(i).c_str(), esc, sizeof(esc));
    o += snprintf(buf + o, sizeof(buf) - o, "{\"ssid\":\"%s\",\"rssi\":%d,\"secure\":%d}", esc, WiFi.RSSI(i), WiFi.encryptionType(i) != WIFI_AUTH_OPEN ? 1 : 0);
  }
  snprintf(buf + o, sizeof(buf) - o, "]}");
  WiFi.scanDelete();
  gWeb.send(200, "application/json", buf);
}

struct SdapDev {
  char ip[18];
  char prod[28];
  char ser[20];
};

static void handleSdapDiscover() {
  if (WiFi.status() != WL_CONNECTED) {
    gWeb.send(400, "application/json", "{\"error\":\"WiFi not connected\"}");
    return;
  }
  int ms = gWeb.arg("ms").toInt();
  if (ms < 500) ms = 500;
  if (ms > 20000) ms = 20000;
  if (!gSdapBegun) {
    if (gSdapUdp.begin(kSdapPort)) gSdapBegun = true;
  }
  if (!gSdapBegun) {
    gWeb.send(500, "application/json", "{\"error\":\"SDAP bind failed (port 53862 in use?)\"}");
    return;
  }
  SdapDev devs[16];
  int nDev = 0;
  unsigned long tEnd = millis() + (unsigned)ms;
  uint8_t pkt[160];
  while (millis() < tEnd) {
    int len = gSdapUdp.parsePacket();
    if (len > 0) {
      int n = gSdapUdp.read(pkt, sizeof(pkt));
      char ip[20], prod[28], ser[20];
      if (n >= 122 && sdapDecode(pkt, n, ip, prod, sizeof(prod), ser, sizeof(ser))) {
        int idx = -1;
        for (int i = 0; i < nDev; i++) {
          if (strcmp(devs[i].ip, ip) == 0) {
            idx = i;
            break;
          }
        }
        if (idx < 0 && nDev < 16) {
          strncpy(devs[nDev].ip, ip, sizeof(devs[0].ip));
          strncpy(devs[nDev].prod, prod, sizeof(devs[0].prod));
          strncpy(devs[nDev].ser, ser, sizeof(devs[0].ser));
          devs[nDev].ip[sizeof(devs[0].ip) - 1] = 0;
          nDev++;
        }
      }
    }
    delay(2);
    gWeb.handleClient();
  }
  char out[1600];
  size_t o = 0;
  o += snprintf(out + o, sizeof(out) - o, "{\"devices\":[");
  for (int i = 0; i < nDev; i++) {
    if (i) out[o++] = ',';
    char pe[56], se[36];
    jsonEscape(devs[i].prod, pe, sizeof(pe));
    jsonEscape(devs[i].ser, se, sizeof(se));
    o += snprintf(out + o, sizeof(out) - o, "{\"ip\":\"%s\",\"product\":\"%s\",\"serial\":\"%s\"}", devs[i].ip, pe, se);
  }
  snprintf(out + o, sizeof(out) - o, "]}");
  gWeb.send(200, "application/json", out);
}

static void handleSaveWifi() {
  if (!gWeb.hasArg("ssid")) {
    gWeb.send(400, "text/plain", "missing ssid");
    return;
  }
  String ssid = gWeb.arg("ssid");
  String pass = gWeb.hasArg("pass") ? gWeb.arg("pass") : "";
  bool passUpdate = gWeb.hasArg("pass") && pass.length() > 0;
  cfgSaveWifi(ssid.c_str(), pass.c_str(), passUpdate);
  gWeb.send(200, "text/plain", "Saved. Rebooting…");
  delay(300);
  ESP.restart();
}

static void handleSaveMonitor() {
  if (!gWeb.hasArg("mhost")) {
    gWeb.send(400, "text/plain", "missing mhost");
    return;
  }
  cfgSaveMonitor(gWeb.arg("mhost").c_str());
  gWeb.send(200, "text/plain", "Monitor IP saved.");
}

static void handleErase() {
  cfgErase();
  gWeb.send(200, "text/plain", "Erased. Rebooting…");
  delay(300);
  ESP.restart();
}

static void handleRoot() { gWeb.send_P(200, "text/html", CONFIG_PORTAL_HTML); }

static void webRegisterRoutes() {
  gWeb.on("/", HTTP_GET, handleRoot);
  gWeb.on("/api/status", HTTP_GET, handleApiStatus);
  gWeb.on("/api/wifi-scan", HTTP_GET, handleWifiScan);
  gWeb.on("/api/sdap-discover", HTTP_GET, handleSdapDiscover);
  gWeb.on("/api/save-wifi", HTTP_POST, handleSaveWifi);
  gWeb.on("/api/save-monitor", HTTP_POST, handleSaveMonitor);
  gWeb.on("/api/erase-config", HTTP_POST, handleErase);
}

static void startApPortal() {
  gPortalApMode = true;
  sdapStop();
  WiFi.disconnect(true);
  delay(200);
  WiFi.mode(WIFI_AP);
  uint8_t mac[6];
  WiFi.macAddress(mac);
  char apName[32];
  snprintf(apName, sizeof(apName), "MonitorCtrl-%02X%02X", mac[4], mac[5]);
  strncpy(gApSsidBuf, apName, sizeof(gApSsidBuf) - 1);
  gApSsidBuf[sizeof(gApSsidBuf) - 1] = 0;
  WiFi.softAP(apName, "monitorctl");
  gDns.start(53, "*", WiFi.softAPIP());
  gWeb.stop();
  webRegisterRoutes();
  gWeb.begin();
  gWebStarted = true;
  Serial.printf("AP \"%s\" pass monitorctl IP %s\n", apName, WiFi.softAPIP().toString().c_str());
}

static void startStaConfigWeb() {
  gPortalApMode = false;
  gApSsidBuf[0] = 0;
  gWeb.stop();
  webRegisterRoutes();
  gWeb.begin(kCfgWebStaPort);
  gWebStarted = true;
  Serial.printf("Config http://%s:%u\n", WiFi.localIP().toString().c_str(), (unsigned)kCfgWebStaPort);
}

bool wifiConnectOrPortal() {
  cfgLoad();
  if (!cfgWifiOk()) {
    startApPortal();
    return false;
  }
  WiFi.mode(WIFI_STA);
  WiFi.begin(gWifiSsid, gHasWifiPass ? gWifiPass : "");
  Serial.printf("WiFi connecting \"%s\"…\n", gWifiSsid);
  unsigned long t0 = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - t0 < 25000) {
    delay(300);
    Serial.print('.');
  }
  Serial.println();
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println(F("WiFi failed, starting config AP."));
    startApPortal();
    return false;
  }
  Serial.println(WiFi.localIP());
  startStaConfigWeb();
  sdapEnsureSocket();
  return true;
}

void webLoop() {
  if (!gWebStarted) return;
  if (gPortalApMode) gDns.processNextRequest();
  gWeb.handleClient();
}

bool wifiPortalActive(void) { return gPortalApMode; }

void serialDiscoverSdap(unsigned long ms) {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println(F("WiFi not connected."));
    return;
  }
  sdapEnsureSocket();
  Serial.printf("SDAP listen UDP %u for %lu ms…\n", (unsigned)kSdapPort, ms);
  unsigned long tEnd = millis() + ms;
  uint8_t pkt[160];
  while (millis() < tEnd) {
    int len = gSdapUdp.parsePacket();
    if (len > 0) {
      int n = gSdapUdp.read(pkt, sizeof(pkt));
      char ip[20], prod[24], ser[20];
      if (n >= 122 && sdapDecode(pkt, n, ip, prod, sizeof(prod), ser, sizeof(ser)))
        Serial.printf("  %s  %s  #%s\n", ip, prod, ser);
    }
    delay(2);
  }
}

void enterPortalFromRunning() {
  Serial.println(F("Switching to config AP…"));
  startApPortal();
}
