/* global window, document, fetch */
(function (window) {
  'use strict';

  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function licenceHeaders(token) {
    var headers = { Accept: 'application/json', 'Content-Type': 'application/json' };
    if (token && token.value) headers.RequestVerificationToken = token.value;
    return headers;
  }

  function renderLicence(statusEl, data) {
    if (!statusEl) return;
    var licence = data && data.licence ? data.licence : data || {};
    statusEl.textContent = JSON.stringify(
      {
        status: pick(licence, 'state', 'State'),
        active: pick(licence, 'isActive', 'IsActive'),
        tier: pick(licence, 'tier', 'Tier'),
        marketplace: pick(licence, 'marketplaceModuleEntitlement', 'MarketplaceModuleEntitlement'),
        workshop: pick(licence, 'workshopPortalEntitlement', 'WorkshopPortalEntitlement'),
        fleet: pick(licence, 'fleetPortalEntitlement', 'FleetPortalEntitlement'),
        dealer: pick(licence, 'dealerPortalEntitlement', 'DealerPortalEntitlement'),
        lastHeartbeatUtc: pick(licence, 'lastHeartbeatUtc', 'LastHeartbeatUtc'),
        reasonCode: pick(licence, 'reasonCode', 'ReasonCode')
      },
      null,
      2
    );
  }

  function initLicencePanel(root) {
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    var statusEl = root.querySelector('[data-ce-licence-status]');
    var keyEl = root.querySelector('[data-ce-licence-key]');
    var activateBtn = root.querySelector('[data-ce-licence-activate]');
    var heartbeatBtn = root.querySelector('[data-ce-licence-heartbeat]');
    var alertEl = root.querySelector('[data-ce-licence-alert]');

    function showAlert(kind, message) {
      if (!alertEl) return;
      alertEl.hidden = !message;
      alertEl.textContent = message || '';
      alertEl.className = 'alert alert-' + (kind === 'error' ? 'danger' : kind === 'success' ? 'success' : 'info');
    }

    function refreshLicence() {
      return fetch('/Admin/CheckEngine/DiagnosticsAdmin/Package', { headers: licenceHeaders(token) })
        .then(function (r) {
          return r.json();
        })
        .then(function (data) {
          renderLicence(statusEl, data);
        })
        .catch(function () {
          if (statusEl) statusEl.textContent = 'Unable to load licence status.';
        });
    }

    if (activateBtn) {
      activateBtn.addEventListener('click', function () {
        showAlert('info', '');
        fetch('/Admin/CheckEngine/DiagnosticsAdmin/Activate', {
          method: 'POST',
          headers: licenceHeaders(token),
          body: JSON.stringify({ licenceKey: keyEl ? keyEl.value : '' })
        })
          .then(function (r) {
            return r.json().then(function (body) {
              if (!r.ok) throw body;
              return body;
            });
          })
          .then(function (status) {
            renderLicence(statusEl, status);
            showAlert('success', 'Licence activated.');
          })
          .catch(function (err) {
            showAlert('error', (err && (err.reasonCode || err.errorCode)) || 'Activation failed.');
            refreshLicence();
          });
      });
    }

    if (heartbeatBtn) {
      heartbeatBtn.addEventListener('click', function () {
        fetch('/Admin/CheckEngine/DiagnosticsAdmin/Heartbeat', {
          method: 'POST',
          headers: licenceHeaders(token)
        })
          .then(function (r) {
            return r.json();
          })
          .then(function (status) {
            renderLicence(statusEl, status);
            showAlert('success', 'Heartbeat recorded.');
          })
          .catch(refreshLicence);
      });
    }

    refreshLicence();
  }

  function boot() {
    document.querySelectorAll('[data-ce-licence-panel]').forEach(initLicencePanel);
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();

  window.CheckEngineLicenceAdmin = { initLicencePanel: initLicencePanel };
})(window);
