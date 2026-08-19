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

  function yn(value) {
    if (value === true || value === 'true') return 'Yes';
    if (value === false || value === 'false') return 'No';
    return value == null || value === '' ? '—' : String(value);
  }

  function setCell(root, selector, value) {
    var el = root.querySelector(selector);
    if (el) el.textContent = value == null || value === '' ? '—' : String(value);
  }

  function renderLicence(root, data) {
    if (!root) return;
    var licence = (data && (data.licence || data.Licence)) || data || {};
    setCell(root, '[data-ce-licence-state]', pick(licence, 'state', 'State'));
    setCell(root, '[data-ce-licence-active]', yn(pick(licence, 'isActive', 'IsActive')));
    setCell(root, '[data-ce-licence-tier]', pick(licence, 'tier', 'Tier'));
    setCell(root, '[data-ce-licence-marketplace]', yn(pick(licence, 'marketplaceModuleEntitlement', 'MarketplaceModuleEntitlement')));
    setCell(root, '[data-ce-licence-workshop]', yn(pick(licence, 'workshopPortalEntitlement', 'WorkshopPortalEntitlement')));
    setCell(root, '[data-ce-licence-fleet]', yn(pick(licence, 'fleetPortalEntitlement', 'FleetPortalEntitlement')));
    setCell(root, '[data-ce-licence-dealer]', yn(pick(licence, 'dealerPortalEntitlement', 'DealerPortalEntitlement')));
    setCell(root, '[data-ce-licence-heartbeat]', pick(licence, 'lastHeartbeatUtc', 'LastHeartbeatUtc'));
    setCell(root, '[data-ce-licence-reason]', pick(licence, 'reasonCode', 'ReasonCode'));
  }

  function initLicencePanel(root) {
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
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
          renderLicence(root, data);
        })
        .catch(function () {
          setCell(root, '[data-ce-licence-state]', 'Unable to load licence status.');
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
            renderLicence(root, status);
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
            renderLicence(root, status);
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
