const $ = (id) => document.getElementById(id);

function hostBody() {
  const host = $("host").value.trim();
  if (!host) throw new Error("Enter monitor host IP.");
  const timeoutMs = Number.parseInt($("timeout").value, 10);
  return { host, timeoutMs: Number.isFinite(timeoutMs) ? timeoutMs : undefined };
}

/** Optional fields for VMC routes and live polling (matches REST / SSE query names). */
function vmcTransportOptions() {
  const o = {};
  const su = Number.parseInt($("sdcpUnit").value, 10);
  if (Number.isFinite(su) && su >= 0 && su <= 255) o.sdcpUnitId = su;
  const vi = $("vmcItem").value.trim();
  if (vi) o.vmcItem = vi;
  return o;
}

function fwHeaders() {
  const h = { "Content-Type": "application/json" };
  if ($("fwAck").checked) h["X-Firmware-Ack"] = "CONFIRM";
  return h;
}

async function api(method, path, body, headers) {
  const res = await fetch(path, {
    method,
    headers: headers ?? { "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  if (!res.ok) {
    const err = new Error(typeof json === "string" ? json : JSON.stringify(json, null, 2));
    err.detail = json;
    throw err;
  }
  return json;
}

function show(el, data) {
  el.textContent = typeof data === "string" ? data : JSON.stringify(data, null, 2);
}

$("btnDiscover").onclick = async () => {
  const ms = $("discoverMs").value;
  const bind = $("bind").value.trim();
  let q = `durationMs=${encodeURIComponent(ms)}`;
  if (bind) q += `&bind=${encodeURIComponent(bind)}`;
  try {
    const data = await api("GET", `/api/sdap/discover?${q}`);
    show($("outDiscover"), data);
  } catch (e) {
    show($("outDiscover"), String(e.message));
  }
};

$("btnVmcGet").onclick = async () => {
  try {
    const data = await api("POST", "/api/vmc/get", {
      ...hostBody(),
      ...vmcTransportOptions(),
      field: $("vmcField").value.trim(),
    });
    show($("outVmc"), data);
  } catch (e) {
    show($("outVmc"), String(e.message));
  }
};

$("btnVmcSet").onclick = async () => {
  const tail = $("vmcSet").value.trim().split(/\s+/).filter(Boolean);
  try {
    const data = await api("POST", "/api/vmc/set", { ...hostBody(), ...vmcTransportOptions(), args: tail });
    show($("outVmc"), data);
  } catch (e) {
    show($("outVmc"), String(e.message));
  }
};

/** @type {EventSource | null} */
let sseMonitor = null;

function stopSse() {
  if (sseMonitor) {
    sseMonitor.close();
    sseMonitor = null;
  }
}

$("btnSseStart").onclick = () => {
  const host = $("host").value.trim();
  if (!host) {
    show($("outSse"), "Enter monitor host IP in Connection.");
    return;
  }
  stopSse();
  const intervalMs = Number.parseInt($("sseMs").value, 10);
  const fields = $("sseFields").value.trim();
  const q = new URLSearchParams({ host });
  if (Number.isFinite(intervalMs)) q.set("intervalMs", String(intervalMs));
  if (fields) q.set("fields", fields);
  const su = $("sdcpUnit").value.trim();
  if (su) q.set("sdcpUnitId", su);
  const vi = $("vmcItem").value.trim();
  if (vi) q.set("vmcItem", vi);
  sseMonitor = new EventSource(`/api/events/monitor?${q}`);
  sseMonitor.onmessage = (ev) => {
    try {
      show($("outSse"), JSON.parse(ev.data));
    } catch {
      show($("outSse"), ev.data);
    }
  };
  sseMonitor.addEventListener("fault", (ev) => {
    show($("outSse"), `fault: ${ev.data}`);
  });
};

$("btnSseStop").onclick = () => {
  stopSse();
  show($("outSse"), "(stopped)");
};

$("btnVms").onclick = async () => {
  try {
    const data = await api("POST", "/api/vms/product-info", hostBody());
    show($("outVms"), data);
  } catch (e) {
    show($("outVms"), String(e.message));
  }
};

async function vma(path) {
  try {
    const data = await api("POST", path, hostBody());
    show($("outVma"), data);
  } catch (e) {
    show($("outVma"), String(e.message));
  }
}

$("btnVmaSw").onclick = () => vma("/api/vma/control-software-version");
$("btnVmaKer").onclick = () => vma("/api/vma/kernel-version");
$("btnVmaRtc").onclick = () => vma("/api/vma/rtc");
$("btnVmaFpga1").onclick = () => vma("/api/vma/fpga1-version");
$("btnVmaFpga2").onclick = () => vma("/api/vma/fpga2-version");
$("btnVmaFpgaC").onclick = () => vma("/api/vma/fpga-core-version");

$("btnFwKer").onclick = async () => {
  const size = Number.parseInt($("fwKer").value, 10);
  try {
    const data = await api(
      "POST",
      "/api/vma/firmware/upgrade-kernel-size",
      { ...hostBody(), sizeBytes: size },
      fwHeaders(),
    );
    show($("outFw"), data);
  } catch (e) {
    show($("outFw"), String(e.message));
  }
};

$("btnFwFpga").onclick = async () => {
  const size = Number.parseInt($("fwFpga").value, 10);
  try {
    const data = await api(
      "POST",
      "/api/vma/firmware/upgrade-fpga-size",
      { ...hostBody(), sizeBytes: size },
      fwHeaders(),
    );
    show($("outFw"), data);
  } catch (e) {
    show($("outFw"), String(e.message));
  }
};

$("btnFwChunk").onclick = async () => {
  const chunk = Number.parseInt($("fwChunk").value, 10);
  try {
    const data = await api(
      "POST",
      "/api/vma/firmware/upgrade-chunk",
      { ...hostBody(), chunkIndex: chunk },
      fwHeaders(),
    );
    show($("outFw"), data);
  } catch (e) {
    show($("outFw"), String(e.message));
  }
};

$("btnFwRestart").onclick = async () => {
  try {
    const data = await api("POST", "/api/vma/firmware/upgrade-restart", hostBody(), fwHeaders());
    show($("outFw"), data);
  } catch (e) {
    show($("outFw"), String(e.message));
  }
};
