namespace BeeKingdom.Server;

// Minimal, dependency-free HTML+JS admin/support page for Jeff: look up a player, inspect their
// hive resources/roster/combat patrol slots, and manually adjust them for bug fixes. Gated
// server-side by AdminSupportOptions/AuthorizeAdminSupport; the support key is only ever kept in
// a page-local JS variable (never persisted to a cookie/localStorage) and sent as the
// X-BeeKingdom-Support-Key header on every request.
public static class AdminUiPage
{
    public const string Html = """
<!doctype html>
<html lang="fr">
<head>
<meta charset="utf-8" />
<title>Bee Kingdom — Support</title>
<style>
  body { font-family: -apple-system, Segoe UI, sans-serif; background:#141210; color:#f1e6d2; margin:0; padding:24px; }
  h1 { font-size: 20px; margin: 0 0 16px; }
  h2 { font-size: 15px; color:#e0a94a; margin: 24px 0 8px; }
  input, button, select { font-size: 14px; padding: 6px 8px; border-radius: 4px; border: 1px solid #574a30; background:#211c16; color:#f1e6d2; }
  button { background:#7a5620; cursor:pointer; }
  button:hover { background:#946a2a; }
  table { border-collapse: collapse; width: 100%; margin-bottom: 12px; }
  td, th { border: 1px solid #3a3227; padding: 4px 8px; text-align: left; font-size: 13px; }
  .row { display:flex; gap:8px; align-items:center; margin-bottom: 8px; flex-wrap: wrap; }
  .hidden { display:none; }
  .err { color:#ff8080; font-size:13px; }
  .hive-btn { margin-right: 6px; margin-bottom: 6px; }
  #login-panel { max-width: 420px; }
</style>
</head>
<body>
<h1>Bee Kingdom — Outil de support interne</h1>

<div id="login-panel">
  <div class="row">
    <input id="key-input" type="password" placeholder="Cle de support (X-BeeKingdom-Support-Key)" style="width:320px" />
    <button onclick="doLogin()">Connexion</button>
  </div>
  <div id="login-error" class="err"></div>
</div>

<div id="main-panel" class="hidden">
  <h2>Recherche joueur</h2>
  <div class="row">
    <input id="email-input" type="text" placeholder="email du joueur" style="width:280px" />
    <button onclick="lookupPlayer()">Rechercher</button>
  </div>
  <div id="lookup-error" class="err"></div>
  <div id="lookup-result"></div>

  <div id="hives-section" class="hidden">
    <h2>Ruches</h2>
    <div id="hive-list"></div>
  </div>

  <div id="hive-detail" class="hidden">
    <h2>Ressources</h2>
    <table id="resources-table"></table>
    <div class="row">
      <select id="res-key"></select>
      <input id="res-delta" type="number" placeholder="delta (+/-)" style="width:100px" />
      <input id="res-reason" type="text" placeholder="motif (obligatoire)" style="width:240px" />
      <button onclick="adjustResource()">Ajuster</button>
    </div>

    <h2>Batiments</h2>
    <table id="buildings-table"></table>
    <div class="row">
      <input id="building-key" type="text" placeholder="cle du batiment (ex: nursery_cluster)" style="width:220px" />
      <input id="building-level" type="number" min="0" placeholder="niveau" style="width:100px" />
      <input id="building-reason" type="text" placeholder="motif (obligatoire)" style="width:240px" />
      <button onclick="setBuildingLevel()">Fixer le niveau</button>
    </div>

    <h2>Effectifs (roster)</h2>
    <table id="roster-table"></table>
    <div class="row">
      <select id="roster-family">
        <option value="guardians">guardians</option>
        <option value="wingrunners">wingrunners</option>
        <option value="darters">darters</option>
      </select>
      <input id="roster-delta" type="number" placeholder="delta (+/-)" style="width:100px" />
      <input id="roster-reason" type="text" placeholder="motif (obligatoire)" style="width:240px" />
      <button onclick="adjustRoster()">Ajuster</button>
    </div>

    <h2>Patrouilles de combat</h2>
    <div id="patrol-summary"></div>
    <div class="row">
      <input id="slot-reason" type="text" placeholder="motif (obligatoire)" style="width:240px" />
      <button onclick="grantSlot(false)">Accorder un emplacement (ressource)</button>
      <button onclick="grantSlot(true)">Accorder un emplacement (premium)</button>
    </div>

    <h2>Historique d'achats brut</h2>
    <div id="purchase-history">(non charge — voir compte joueur)</div>

    <h2>Journal d'audit de cette ruche</h2>
    <table id="audit-table"></table>
  </div>
  <div id="detail-error" class="err"></div>
</div>

<script>
let supportKey = "";
let currentPlayerId = "";
let currentHiveId = "";

function doLogin() {
  supportKey = document.getElementById("key-input").value.trim();
  if (!supportKey) { document.getElementById("login-error").textContent = "Cle requise."; return; }
  document.getElementById("login-panel").classList.add("hidden");
  document.getElementById("main-panel").classList.remove("hidden");
}

async function api(path, options) {
  options = options || {};
  options.headers = Object.assign({ "X-BeeKingdom-Support-Key": supportKey, "Content-Type": "application/json" }, options.headers || {});
  const response = await fetch(path, options);
  const text = await response.text();
  let body = null;
  try { body = text ? JSON.parse(text) : null; } catch (e) { body = text; }
  if (!response.ok) throw new Error((body && body.code) ? body.code : ("HTTP " + response.status));
  return body;
}

async function lookupPlayer() {
  document.getElementById("lookup-error").textContent = "";
  document.getElementById("hives-section").classList.add("hidden");
  document.getElementById("hive-detail").classList.add("hidden");
  const email = document.getElementById("email-input").value.trim();
  try {
    const result = await api("/admin/v1/players/lookup?email=" + encodeURIComponent(email));
    currentPlayerId = result.playerId;
    document.getElementById("lookup-result").textContent = "playerId: " + result.playerId + " | statut: " + result.status;
    const hives = await api("/admin/v1/players/" + currentPlayerId + "/hives");
    const list = document.getElementById("hive-list");
    list.innerHTML = "";
    (hives.hiveIds || []).forEach(function (hiveId) {
      const btn = document.createElement("button");
      btn.className = "hive-btn";
      btn.textContent = hiveId;
      btn.onclick = function () { loadHive(hiveId); };
      list.appendChild(btn);
    });
    document.getElementById("hives-section").classList.remove("hidden");
  } catch (e) {
    document.getElementById("lookup-error").textContent = "Erreur: " + e.message;
  }
}

async function loadHive(hiveId) {
  currentHiveId = hiveId;
  document.getElementById("detail-error").textContent = "";
  try {
    const diag = await api("/admin/v1/players/" + currentPlayerId + "/hives/" + currentHiveId + "/diagnostics");
    renderDiagnostics(diag);
    document.getElementById("hive-detail").classList.remove("hidden");
  } catch (e) {
    document.getElementById("detail-error").textContent = "Erreur: " + e.message;
  }
}

let lastRevision = 0;

function renderDiagnostics(diag) {
  lastRevision = diag.revision;
  const resTable = document.getElementById("resources-table");
  resTable.innerHTML = "<tr><th>Ressource</th><th>Montant</th><th>Capacite</th></tr>";
  const resSelect = document.getElementById("res-key");
  resSelect.innerHTML = "";
  Object.keys(diag.resources || {}).forEach(function (key) {
    const bal = diag.resources[key];
    resTable.innerHTML += "<tr><td>" + key + "</td><td>" + bal.amount + "</td><td>" + bal.capacity + "</td></tr>";
    const opt = document.createElement("option"); opt.value = key; opt.textContent = key; resSelect.appendChild(opt);
  });

  const buildingsTable = document.getElementById("buildings-table");
  buildingsTable.innerHTML = "<tr><th>Batiment</th><th>Niveau</th></tr>";
  Object.keys(diag.buildingLevels || {}).forEach(function (key) {
    buildingsTable.innerHTML += "<tr><td>" + key + "</td><td>" + diag.buildingLevels[key] + "</td></tr>";
  });

  const rosterTable = document.getElementById("roster-table");
  rosterTable.innerHTML = "<tr><th>Famille</th><th>Effectif</th></tr>";
  Object.keys(diag.roster || {}).forEach(function (key) {
    rosterTable.innerHTML += "<tr><td>" + key + "</td><td>" + diag.roster[key] + "</td></tr>";
  });

  document.getElementById("patrol-summary").textContent =
    "Patrouilles actives: " + diag.combatPatrolActiveCount + " / " + diag.combatPatrolTotalSlots +
    " (emplacements ressource achetes: " + diag.combatPatrolResourcePurchasedSlots + "/2, premium: " + diag.combatPatrolPremiumPurchasedSlots + "/2)";

  const auditTable = document.getElementById("audit-table");
  auditTable.innerHTML = "<tr><th>Date (UTC)</th><th>Action</th><th>Details</th><th>Motif</th></tr>";
  (diag.adminAudit || []).slice().reverse().forEach(function (entry) {
    auditTable.innerHTML += "<tr><td>" + entry.atUtc + "</td><td>" + entry.action + "</td><td>" + entry.details + "</td><td>" + entry.reason + "</td></tr>";
  });
}

async function refreshHive() { await loadHive(currentHiveId); }

async function adjustResource() {
  const resource = document.getElementById("res-key").value;
  const delta = parseInt(document.getElementById("res-delta").value || "0", 10);
  const reason = document.getElementById("res-reason").value.trim();
  if (!reason) { document.getElementById("detail-error").textContent = "Un motif est requis."; return; }
  try {
    await api("/admin/v1/players/" + currentPlayerId + "/hives/" + currentHiveId + "/resources/adjust", {
      method: "POST",
      body: JSON.stringify({ resource: resource, delta: delta, reason: reason, expectedRevision: lastRevision })
    });
    document.getElementById("res-delta").value = "";
    document.getElementById("res-reason").value = "";
    await refreshHive();
  } catch (e) { document.getElementById("detail-error").textContent = "Erreur: " + e.message; }
}

async function adjustRoster() {
  const family = document.getElementById("roster-family").value;
  const delta = parseInt(document.getElementById("roster-delta").value || "0", 10);
  const reason = document.getElementById("roster-reason").value.trim();
  if (!reason) { document.getElementById("detail-error").textContent = "Un motif est requis."; return; }
  try {
    await api("/admin/v1/players/" + currentPlayerId + "/hives/" + currentHiveId + "/roster/adjust", {
      method: "POST",
      body: JSON.stringify({ family: family, delta: delta, reason: reason, expectedRevision: lastRevision })
    });
    document.getElementById("roster-delta").value = "";
    document.getElementById("roster-reason").value = "";
    await refreshHive();
  } catch (e) { document.getElementById("detail-error").textContent = "Erreur: " + e.message; }
}

async function setBuildingLevel() {
  const key = document.getElementById("building-key").value.trim();
  const level = parseInt(document.getElementById("building-level").value || "-1", 10);
  const reason = document.getElementById("building-reason").value.trim();
  if (!key) { document.getElementById("detail-error").textContent = "Une cle de batiment est requise."; return; }
  if (level < 0) { document.getElementById("detail-error").textContent = "Niveau invalide."; return; }
  if (!reason) { document.getElementById("detail-error").textContent = "Un motif est requis."; return; }
  try {
    await api("/admin/v1/players/" + currentPlayerId + "/hives/" + currentHiveId + "/buildings/level", {
      method: "POST",
      body: JSON.stringify({ buildingKey: key, level: level, reason: reason, expectedRevision: lastRevision })
    });
    document.getElementById("building-level").value = "";
    document.getElementById("building-reason").value = "";
    await refreshHive();
  } catch (e) { document.getElementById("detail-error").textContent = "Erreur: " + e.message; }
}

async function grantSlot(premium) {
  const reason = document.getElementById("slot-reason").value.trim();
  if (!reason) { document.getElementById("detail-error").textContent = "Un motif est requis."; return; }
  try {
    await api("/admin/v1/players/" + currentPlayerId + "/hives/" + currentHiveId + "/combat-patrol/slots/grant", {
      method: "POST",
      body: JSON.stringify({ premium: premium, reason: reason, expectedRevision: lastRevision })
    });
    document.getElementById("slot-reason").value = "";
    await refreshHive();
  } catch (e) { document.getElementById("detail-error").textContent = "Erreur: " + e.message; }
}
</script>
</body>
</html>
""";
}
