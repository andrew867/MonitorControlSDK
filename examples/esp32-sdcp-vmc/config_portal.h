/*
 * config_portal.h — HTML5 WiFi / SDAP discovery UI (PROGMEM) for monitor_knobs_sdcp.ino
 * Served on :80 in captive AP mode, :8080 when connected as STA.
 */
#ifndef CONFIG_PORTAL_H
#define CONFIG_PORTAL_H

static const char CONFIG_PORTAL_HTML[] PROGMEM = R"rawliteral(<!DOCTYPE html>
<html lang="en"><head>
<meta charset="utf-8"/><meta name="viewport" content="width=device-width,initial-scale=1"/>
<title>Monitor SDCP — Setup</title>
<style>
:root{--bg:#0f1419;--card:#1a2332;--txt:#e7ecf3;--muted:#8b9bb4;--acc:#3d9cf5;--ok:#3ecf8e;--err:#f55}
*{box-sizing:border-box}body{margin:0;font-family:system-ui,Segoe UI,Roboto,sans-serif;background:var(--bg);color:var(--txt);line-height:1.45;padding:12px;max-width:520px;margin:0 auto}
h1{font-size:1.15rem;margin:0 0 4px}p.sub{color:var(--muted);font-size:.85rem;margin:0 0 16px}
section{background:var(--card);border-radius:12px;padding:14px 16px;margin-bottom:14px;border:1px solid #2a3544}
label{display:block;font-size:.8rem;color:var(--muted);margin:10px 0 4px}
input,select{width:100%;padding:10px;border-radius:8px;border:1px solid #334;border:none;background:#0d1218;color:var(--txt);font-size:16px}
button{margin-top:10px;padding:12px 16px;border-radius:8px;border:none;background:var(--acc);color:#fff;font-weight:600;width:100%;cursor:pointer;font-size:15px}
button.secondary{background:#334}button.danger{background:var(--err)}
.row{display:flex;gap:8px;flex-wrap:wrap}.row button{flex:1;min-width:120px}
pre{white-space:pre-wrap;font-size:11px;background:#0d1218;padding:10px;border-radius:8px;max-height:200px;overflow:auto;color:#b8d}
.status{font-size:.85rem;color:var(--ok)}.err{color:var(--err)}.hint{font-size:.75rem;color:var(--muted);margin-top:8px}
#scanList,#discList{font-size:.8rem}
</style></head><body>
<h1>Monitor control (SDCP)</h1>
<p class="sub">WiFi, SDAP discovery (UDP 53862), and monitor IP. Works on ESP32 / ESP32-S3 class boards.</p>

<section><h2 style="font-size:1rem;margin:0 0 8px">Status</h2>
<div id="st" class="status">Loading…</div>
<p class="hint" id="hint"></p></section>

<section><h2 style="font-size:1rem;margin:0 0 8px">WiFi</h2>
<label>SSID</label><input id="ssid" maxlength="32" autocomplete="off"/>
<label>Password</label><input id="pass" type="password" maxlength="64" autocomplete="new-password"/>
<div class="row"><button type="button" class="secondary" id="btnScan">Scan networks</button></div>
<pre id="scanList"></pre>
<button type="button" id="btnSaveWifi">Save WiFi &amp; reconnect</button></section>

<section><h2 style="font-size:1rem;margin:0 0 8px">Monitor (SDCP TCP 53484)</h2>
<label>Monitor IP (Connection IP from SDAP)</label><input id="mhost" maxlength="39" placeholder="192.168.1.10"/>
<div class="row"><button type="button" class="secondary" id="btnDisc">SDAP discover</button></div>
<pre id="discList"></pre>
<button type="button" id="btnSaveMon">Save monitor IP</button>
<p class="hint">Discovery listens on UDP <strong>53862</strong> for a few seconds; your monitor must be on the same LAN as this ESP32 (not only in AP setup mode).</p></section>

<section><h2 style="font-size:1rem;margin:0 0 8px">Danger zone</h2>
<button type="button" class="danger" id="btnErase">Erase WiFi &amp; reboot to portal</button></section>

<script>
const $=id=>document.getElementById(id);
async function api(p){const r=await fetch(p);if(!r.ok)throw new Error(await r.text());return r.json();}
async function refresh(){
  try{const j=await api('/api/status');
    $('st').textContent='Mode: '+j.mode+' | WiFi: '+(j.wifi||'-')+' | IP: '+(j.ip||'-')+' | Monitor: '+(j.monitor||'-');
    $('hint').textContent=j.apSsid?('If captive: join WiFi "'+j.apSsid+'" then open http://'+j.apIp):('Config UI also: http://'+(j.staIp||j.ip||'?')+':8080');
    $('ssid').value=j.storedSsid||'';$('pass').placeholder=j.hasPass?'(saved — leave blank to keep)':'';
    $('mhost').value=j.monitor||'';
  }catch(e){$('st').innerHTML='<span class="err">'+e+'</span>';}
}
$('btnScan').onclick=async()=>{$('scanList').textContent='Scanning…';try{const j=await api('/api/wifi-scan');
  $('scanList').textContent=j.networks&&j.networks.length?j.networks.map(n=>n.rssi+' dBm  '+n.ssid+(n.secure?' [secured]':' [open]')).join('\n'):'(none)';
}catch(e){$('scanList').textContent=String(e);}};
$('btnDisc').onclick=async()=>{$('discList').textContent='Listening on UDP 53862…';try{const j=await api('/api/sdap-discover?ms=4500');
  if(!j.devices||!j.devices.length){$('discList').textContent='No SDAP packets (check VLAN, firewall, or connect ESP32 to same LAN as monitor).';return;}
  $('discList').textContent=j.devices.map(d=>d.ip+'  '+d.product+'  #'+d.serial).join('\n');
  if(j.devices[0]&&j.devices[0].ip)$('mhost').value=j.devices[0].ip;
}catch(e){$('discList').textContent=String(e);}};
$('btnSaveWifi').onclick=async()=>{const fd=new FormData();fd.set('ssid',$('ssid').value);fd.set('pass',$('pass').value);
  const r=await fetch('/api/save-wifi',{method:'POST',body:fd});$('st').textContent=await r.text();setTimeout(()=>location.reload(),2500);};
$('btnSaveMon').onclick=async()=>{const fd=new FormData();fd.set('mhost',$('mhost').value);
  const r=await fetch('/api/save-monitor',{method:'POST',body:fd});$('st').textContent=await r.text();refresh();};
$('btnErase').onclick=async()=>{if(!confirm('Erase WiFi credentials and reboot?'))return;
  await fetch('/api/erase-config',{method:'POST'});$('st').textContent='Rebooting…';};
refresh();
</script></body></html>)rawliteral";

#endif
