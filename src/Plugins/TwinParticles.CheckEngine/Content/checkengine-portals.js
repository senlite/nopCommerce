/* global window, document, fetch */
(function (window) {
  'use strict';

  function pick(obj, camel, pascal) {
    if (!obj) return undefined;
    if (obj[camel] !== undefined) return obj[camel];
    if (obj[pascal] !== undefined) return obj[pascal];
    return undefined;
  }

  function headers(token, json) {
    var h = { Accept: 'application/json' };
    if (json) h['Content-Type'] = 'application/json';
    if (token && token.value) h.RequestVerificationToken = token.value;
    return h;
  }

  function showAlert(el, kind, message) {
    if (!el) return;
    el.hidden = !message;
    el.textContent = message || '';
    el.className = 'ce-mp-alert ce-mp-alert--' + (kind || 'info');
  }

  function portalErrorMessage(root, err) {
    var code = err && err.errorCode;
    var map = {
      'portal.access_unauthenticated': root.getAttribute('data-ce-error-login'),
      'portal.account_not_provisioned': root.getAttribute('data-ce-error-provision'),
      'workshop.not_found': root.getAttribute('data-ce-error-provision'),
      'fleet.not_found': root.getAttribute('data-ce-error-provision'),
      'dealer.not_found': root.getAttribute('data-ce-error-provision')
    };
    return (code && map[code]) || (err && err.errorCode) || root.getAttribute('data-ce-error-generic') || 'Request failed.';
  }

  function apiGet(url, token) {
    return fetch(url, { headers: headers(token), credentials: 'same-origin' }).then(function (r) {
      return r.json().then(function (body) {
        if (!r.ok) throw body;
        return body;
      });
    });
  }

  function apiPost(url, token, payload) {
    return fetch(url, {
      method: 'POST',
      headers: headers(token, true),
      credentials: 'same-origin',
      body: JSON.stringify(payload || {})
    }).then(function (r) {
      return r.json().then(function (body) {
        if (!r.ok) throw body;
        return body;
      });
    });
  }

  function statusBadge(status) {
    var label = status;
    if (typeof status === 'number') {
      var map = { 0: 'Draft', 10: 'In progress', 20: 'Awaiting parts', 30: 'Ready', 40: 'Invoiced', 90: 'Cancelled' };
      label = map[status] || String(status);
    }
    return '<span class="ce-mp-badge">' + label + '</span>';
  }

  function initWorkshop(root) {
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-portal-alert]');
    var stats = root.querySelector('[data-ce-workshop-stats]');
    var jobsBody = root.querySelector('[data-ce-workshop-jobs]');
    var detailPanel = root.querySelector('[data-ce-workshop-detail]');
    var detailSummary = root.querySelector('[data-ce-workshop-detail-summary]');
    var vehiclesBody = root.querySelector('[data-ce-workshop-vehicles]');
    var linesBody = root.querySelector('[data-ce-workshop-lines]');
    var state = { accountId: null, jobs: [], selectedJobId: null };

    function render() {
      if (!stats) return;
      var account = state.account || {};
      var open = state.jobs.filter(function (j) {
        var s = pick(j, 'status', 'Status');
        return s !== 40 && s !== 90;
      }).length;
      stats.innerHTML =
        '<div class="ce-mp-stat"><span class="ce-mp-stat__label">Account</span><span class="ce-mp-stat__value">' +
        (pick(account, 'displayName', 'DisplayName') || '—') +
        '</span></div>' +
        '<div class="ce-mp-stat"><span class="ce-mp-stat__label">Open jobs</span><span class="ce-mp-stat__value">' +
        open +
        '</span></div>' +
        '<div class="ce-mp-stat"><span class="ce-mp-stat__label">Credit used</span><span class="ce-mp-stat__value">' +
        (pick(account, 'creditUsed', 'CreditUsed') || 0) +
        '</span></div>' +
        '<div class="ce-mp-stat"><span class="ce-mp-stat__label">Credit limit</span><span class="ce-mp-stat__value">' +
        (pick(account, 'creditLimit', 'CreditLimit') || 0) +
        '</span></div>';
    }

    function renderJobs() {
      if (!jobsBody) return;
      if (!state.jobs.length) {
        jobsBody.innerHTML = '<tr><td colspan="5">No jobs yet.</td></tr>';
        return;
      }
      jobsBody.innerHTML = state.jobs
        .map(function (job) {
          var id = pick(job, 'id', 'Id');
          var status = pick(job, 'status', 'Status');
          var statusId = typeof status === 'number' ? status : pick(job, 'statusId', 'StatusId');
            return (
            '<tr data-job-id="' +
            id +
            '"><td>#' +
            id +
            '</td><td>' +
            statusBadge(statusId) +
            '</td><td>' +
            (pick(job, 'labourEstimate', 'LabourEstimate') || 0) +
            '</td><td>' +
            (pick(job, 'orderId', 'OrderId') || '—') +
            '</td><td><button type="button" class="ce-btn ce-btn--ghost" data-ce-workshop-view="' +
            id +
            '">View</button> <button type="button" class="ce-btn ce-btn--ghost" data-ce-workshop-start="' +
            id +
            '">Start</button> <button type="button" class="ce-btn ce-btn--ghost" data-ce-workshop-ready="' +
            id +
            '">Mark ready</button> <button type="button" class="ce-btn ce-btn--ghost" data-ce-workshop-cancel="' +
            id +
            '">Cancel</button> <button type="button" class="ce-btn ce-btn--primary" data-ce-workshop-invoice="' +
            id +
            '">Invoice</button></td></tr>'
          );
        })
        .join('');
    }

    function renderJobDetail(detail) {
      if (!detailPanel) return;
      var job = pick(detail, 'job', 'Job') || {};
      var jobId = pick(job, 'id', 'Id');
      state.selectedJobId = jobId;
      detailPanel.hidden = false;
      if (detailSummary) {
        detailSummary.textContent =
          'Job #' + jobId + ' — ' + statusBadge(pick(job, 'status', 'Status'));
      }

      var vehicles = pick(detail, 'vehicles', 'Vehicles') || [];
      if (vehiclesBody) {
        if (!vehicles.length) {
          vehiclesBody.innerHTML = '<tr><td colspan="6">No vehicles on this job.</td></tr>';
        } else {
          vehiclesBody.innerHTML = vehicles
            .map(function (v) {
              var vehicleId = pick(v, 'id', 'Id');
              return (
                '<tr><td>' +
                vehicleId +
                '</td><td>' +
                (pick(v, 'vehicleConfigurationId', 'VehicleConfigurationId') || '—') +
                '</td><td class="ce-code">' +
                (pick(v, 'vin', 'Vin') || '—') +
                '</td><td><input type="number" min="1" class="ce-mp-input ce-code" data-ce-workshop-product="' +
                vehicleId +
                '" style="width:5rem" /></td><td><input type="number" min="1" value="1" class="ce-mp-input" data-ce-workshop-qty="' +
                vehicleId +
                '" style="width:4rem" /></td><td><button type="button" class="ce-btn ce-btn--primary" data-ce-workshop-allocate="' +
                vehicleId +
                '">Allocate</button></td></tr>'
              );
            })
            .join('');
        }
      }

      var lines = pick(detail, 'lines', 'Lines') || [];
      if (linesBody) {
        if (!lines.length) {
          linesBody.innerHTML = '<tr><td colspan="5">No lines allocated yet.</td></tr>';
        } else {
          linesBody.innerHTML = lines
            .map(function (line) {
              return (
                '<tr><td>' +
                (pick(line, 'id', 'Id') || '') +
                '</td><td>' +
                (pick(line, 'productId', 'ProductId') || '') +
                '</td><td>' +
                (pick(line, 'quantity', 'Quantity') || '') +
                '</td><td>' +
                (pick(line, 'fitmentOutcome', 'FitmentOutcome') || '') +
                '</td><td>' +
                (pick(line, 'unitPrice', 'UnitPrice') || 0) +
                '</td></tr>'
              );
            })
            .join('');
        }
      }
    }

    function loadJobDetail(jobId) {
      return apiGet('/check-engine/workshop/JobDetail?jobId=' + encodeURIComponent(jobId), token)
        .then(renderJobDetail)
        .catch(function (err) {
          showAlert(alert, 'error', portalErrorMessage(root, err));
        });
    }

    function refresh() {
      return apiGet('/check-engine/workshop/DashboardData', token)
        .then(function (data) {
          state.account = pick(data, 'account', 'Account');
          state.accountId = pick(state.account, 'id', 'Id');
          state.jobs = pick(data, 'jobs', 'Jobs') || [];
          render();
          renderJobs();
        })
        .catch(function (err) {
          showAlert(alert, 'error', portalErrorMessage(root, err));
        });
    }

    root.addEventListener('click', function (e) {
      var view = e.target.closest('[data-ce-workshop-view]');
      if (view) {
        loadJobDetail(Number(view.getAttribute('data-ce-workshop-view')));
        return;
      }
      var allocate = e.target.closest('[data-ce-workshop-allocate]');
      if (allocate && state.selectedJobId) {
        var vehicleId = Number(allocate.getAttribute('data-ce-workshop-allocate'));
        var productId = Number(root.querySelector('[data-ce-workshop-product="' + vehicleId + '"]')?.value || 0);
        var quantity = Number(root.querySelector('[data-ce-workshop-qty="' + vehicleId + '"]')?.value || 1);
        apiPost('/check-engine/workshop/AllocateJobLine', token, {
          jobId: state.selectedJobId,
          jobVehicleId: vehicleId,
          productId: productId,
          quantity: quantity
        })
          .then(function () {
            return loadJobDetail(state.selectedJobId);
          })
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Allocate failed.');
          });
        return;
      }
      var start = e.target.closest('[data-ce-workshop-start]');
      if (start) {
        apiPost('/check-engine/workshop/TransitionJobStatus', token, {
          jobId: Number(start.getAttribute('data-ce-workshop-start')),
          targetStatus: 10
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Transition failed.');
          });
        return;
      }
      var ready = e.target.closest('[data-ce-workshop-ready]');
      if (ready) {
        apiPost('/check-engine/workshop/TransitionJobStatus', token, {
          jobId: Number(ready.getAttribute('data-ce-workshop-ready')),
          targetStatus: 30
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Transition failed.');
          });
        return;
      }
      var cancel = e.target.closest('[data-ce-workshop-cancel]');
      if (cancel) {
        apiPost('/check-engine/workshop/TransitionJobStatus', token, {
          jobId: Number(cancel.getAttribute('data-ce-workshop-cancel')),
          targetStatus: 90
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Transition failed.');
          });
        return;
      }
      var invoice = e.target.closest('[data-ce-workshop-invoice]');
      if (invoice) {
        apiPost('/check-engine/workshop/RaiseJobInvoice', token, {
          jobId: Number(invoice.getAttribute('data-ce-workshop-invoice'))
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Invoice failed.');
          });
      }
    });

    var createBtn = root.querySelector('[data-ce-workshop-create]');
    if (createBtn) {
      createBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        var rawVehicles = (root.querySelector('[data-ce-workshop-vehicle]')?.value || '')
          .split(/[\s,]+/)
          .map(function (v) {
            return Number(v);
          })
          .filter(function (n) {
            return n > 0;
          });
        var labour = Number(root.querySelector('[data-ce-workshop-labour]')?.value || 0);
        apiPost('/check-engine/workshop/CreateJob', token, {
          workshopAccountId: state.accountId,
          labourEstimate: labour,
          vehicles: rawVehicles.map(function (id) {
            return { vehicleConfigurationId: id };
          })
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Create job failed.');
          });
      });
    }

    refresh();
  }

  function initFleet(root) {
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-portal-alert]');
    var vehiclesBody = root.querySelector('[data-ce-fleet-vehicles]');
    var approvalsBody = root.querySelector('[data-ce-fleet-approvals]');
    var forecastsBody = root.querySelector('[data-ce-fleet-forecasts]');
    var costsBody = root.querySelector('[data-ce-fleet-costs]');
    var importsBody = root.querySelector('[data-ce-fleet-imports]');
    var state = { accountId: null, budgetCentres: [], vehicles: [] };

    function renderVehicles(vehicles) {
      if (!vehiclesBody) return;
      vehicles = vehicles || [];
      if (!vehicles.length) {
        vehiclesBody.innerHTML = '<tr><td colspan="4">No vehicles registered.</td></tr>';
        return;
      }
      vehiclesBody.innerHTML = vehicles
        .map(function (v) {
          return (
            '<tr><td>' +
            (pick(v, 'id', 'Id') || '') +
            '</td><td class="ce-code">' +
            (pick(v, 'vin', 'Vin') || '—') +
            '</td><td>' +
            (pick(v, 'vehicleConfigurationId', 'VehicleConfigurationId') || '—') +
            '</td><td>' +
            (pick(v, 'assetTag', 'AssetTag') || '—') +
            '</td></tr>'
          );
        })
        .join('');
    }

    function renderApprovals(requests) {
      if (!approvalsBody) return;
      requests = requests || [];
      if (!requests.length) {
        approvalsBody.innerHTML = '<tr><td colspan="5">No approval requests.</td></tr>';
        return;
      }
      approvalsBody.innerHTML = requests
        .map(function (r) {
          var id = pick(r, 'id', 'Id');
          var status = pick(r, 'status', 'Status');
          var statusId = typeof status === 'number' ? status : pick(r, 'statusId', 'StatusId');
          var actions =
            statusId === 0
              ? '<button type="button" class="ce-btn ce-btn--primary" data-ce-fleet-approve="' +
                id +
                '">Approve</button> <button type="button" class="ce-btn ce-btn--ghost" data-ce-fleet-reject="' +
                id +
                '">Reject</button>'
              : '—';
          return (
            '<tr><td>#' +
            id +
            '</td><td>' +
            (pick(r, 'productId', 'ProductId') || '') +
            '</td><td>' +
            (pick(r, 'quantity', 'Quantity') || '') +
            '</td><td>' +
            statusBadge(statusId) +
            '</td><td>' +
            actions +
            '</td></tr>'
          );
        })
        .join('');
    }

    function renderForecasts(forecasts) {
      if (!forecastsBody) return;
      forecasts = forecasts || [];
      if (!forecasts.length) {
        forecastsBody.innerHTML = '<tr><td colspan="3">No maintenance forecasts yet. Import VINs to populate.</td></tr>';
        return;
      }
      forecastsBody.innerHTML = forecasts
        .map(function (f) {
          var due = pick(f, 'dueUtc', 'DueUtc') || '';
          return (
            '<tr><td>' +
            (pick(f, 'fleetVehicleId', 'FleetVehicleId') || '') +
            '</td><td>' +
            (pick(f, 'serviceLabel', 'ServiceLabel') || '') +
            '</td><td>' +
            (due ? String(due).slice(0, 10) : '—') +
            '</td></tr>'
          );
        })
        .join('');
    }

    function renderCosts(costs) {
      if (!costsBody) return;
      costs = costs || [];
      if (!costs.length) {
        costsBody.innerHTML = '<tr><td colspan="4">No spend recorded yet.</td></tr>';
        return;
      }
      costsBody.innerHTML = costs
        .map(function (c) {
          return (
            '<tr><td>' +
            (pick(c, 'fleetVehicleId', 'FleetVehicleId') || '') +
            '</td><td class="ce-code">' +
            (pick(c, 'vin', 'Vin') || '—') +
            '</td><td>' +
            (pick(c, 'totalSpend', 'TotalSpend') || 0) +
            '</td><td>' +
            (pick(c, 'orderCount', 'OrderCount') || 0) +
            '</td></tr>'
          );
        })
        .join('');
    }

    function refresh() {
      return apiGet('/check-engine/fleet/DashboardData', token)
        .then(function (data) {
          var account = pick(data, 'account', 'Account');
          state.accountId = pick(account, 'id', 'Id');
          state.budgetCentres = pick(data, 'budgetCentres', 'BudgetCentres') || [];
          state.vehicles = pick(data, 'vehicles', 'Vehicles') || [];
          renderVehicles(state.vehicles);
          renderApprovals(pick(data, 'approvalRequests', 'ApprovalRequests'));
          renderForecasts(pick(data, 'maintenanceForecasts', 'MaintenanceForecasts'));
          renderCosts(pick(data, 'vehicleCostSummaries', 'VehicleCostSummaries'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', portalErrorMessage(root, err));
        });
    }

    root.addEventListener('click', function (e) {
      var approve = e.target.closest('[data-ce-fleet-approve]');
      if (approve) {
        apiPost('/check-engine/fleet/DecideApprovalRequest', token, {
          requestId: Number(approve.getAttribute('data-ce-fleet-approve')),
          approve: true
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Approval failed.');
          });
        return;
      }
      var reject = e.target.closest('[data-ce-fleet-reject]');
      if (reject) {
        apiPost('/check-engine/fleet/DecideApprovalRequest', token, {
          requestId: Number(reject.getAttribute('data-ce-fleet-reject')),
          approve: false,
          rejectionReason: 'Rejected from portal'
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Rejection failed.');
          });
      }
    });

    var importBtn = root.querySelector('[data-ce-fleet-import]');
    if (importBtn) {
      importBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        var raw = (root.querySelector('[data-ce-fleet-vins]')?.value || '').split(/\s+/).filter(Boolean);
        apiPost('/check-engine/fleet/ImportFleetVins', token, {
          fleetAccountId: state.accountId,
          vins: raw
        })
          .then(function () {
            return apiGet('/check-engine/fleet/ImportBatches', token).then(function (batches) {
              if (importsBody) {
                batches = batches || [];
                importsBody.innerHTML = batches.length
                  ? batches
                      .map(function (b) {
                        return (
                          '<tr><td>#' +
                          (pick(b, 'id', 'Id') || '') +
                          '</td><td>' +
                          (pick(b, 'succeededRows', 'SucceededRows') || 0) +
                          '/' +
                          (pick(b, 'totalRows', 'TotalRows') || 0) +
                          '</td><td>' +
                          String(pick(b, 'createdUtc', 'CreatedUtc') || '').slice(0, 10) +
                          '</td></tr>'
                        );
                      })
                      .join('')
                  : '<tr><td colspan="3">No import batches yet.</td></tr>';
              }
              return refresh();
            });
          })
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Import failed.');
          });
      });
    }

    var submitApprovalBtn = root.querySelector('[data-ce-fleet-submit-approval]');
    if (submitApprovalBtn) {
      submitApprovalBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        apiPost('/check-engine/fleet/SubmitApprovalRequest', token, {
          fleetAccountId: state.accountId,
          fleetVehicleId: Number(root.querySelector('[data-ce-fleet-approval-vehicle]')?.value || 0),
          productId: Number(root.querySelector('[data-ce-fleet-approval-product]')?.value || 0),
          quantity: Number(root.querySelector('[data-ce-fleet-approval-qty]')?.value || 1),
          budgetCentreId: Number(root.querySelector('[data-ce-fleet-approval-centre]')?.value || 0)
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Submit approval failed.');
          });
      });
    }

    refresh();
  }

  function initDealer(root) {
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-portal-alert]');
    var catalogBody = root.querySelector('[data-ce-dealer-catalog]');
    var claimsBody = root.querySelector('[data-ce-dealer-claims]');
    var state = { accountId: null, catalog: [] };

    function renderCatalog() {
      if (!catalogBody) return;
      if (!state.catalog.length) {
        catalogBody.innerHTML = '<tr><td colspan="6">No catalog items.</td></tr>';
        return;
      }
      catalogBody.innerHTML = state.catalog
        .map(function (item) {
          var pid = pick(item, 'productId', 'ProductId');
          return (
            '<tr><td>' +
            pid +
            '</td><td class="ce-code">' +
            (pick(item, 'sku', 'Sku') || '') +
            '</td><td>' +
            (pick(item, 'name', 'Name') || '') +
            '</td><td>' +
            (pick(item, 'dealerPrice', 'DealerPrice') || 0) +
            '</td><td>' +
            (pick(item, 'remainingAllocationUnits', 'RemainingAllocationUnits') || '—') +
            '</td><td><input type="number" min="0" class="ce-mp-input" data-ce-dealer-qty="' +
            pid +
            '" value="0" style="width:4rem" /></td></tr>'
          );
        })
        .join('');
    }

    function renderClaims(claims) {
      if (!claimsBody) return;
      claims = claims || [];
      if (!claims.length) {
        claimsBody.innerHTML = '<tr><td colspan="4">No warranty claims.</td></tr>';
        return;
      }
      claimsBody.innerHTML = claims
        .map(function (c) {
          var claimId = pick(c, 'id', 'Id');
          var status = pick(c, 'status', 'Status');
          var statusId = typeof status === 'number' ? status : pick(c, 'statusId', 'StatusId');
          var actions =
            statusId === 0 || statusId === 1
              ? ' <button type="button" class="ce-btn ce-btn--ghost" data-ce-dealer-review="' +
                claimId +
                '">Review</button>'
              : '';
          return (
            '<tr><td>#' +
            claimId +
            '</td><td class="ce-code">' +
            (pick(c, 'oemNumber', 'OemNumber') || '') +
            '</td><td>' +
            statusBadge(statusId) +
            '</td><td>' +
            (pick(c, 'vehicleConfigurationId', 'VehicleConfigurationId') || '') +
            '</td><td>' +
            actions +
            '</td></tr>'
          );
        })
        .join('');
    }

    function refresh() {
      return apiGet('/check-engine/dealer/DashboardData', token)
        .then(function (data) {
          var account = pick(data, 'account', 'Account');
          state.accountId = pick(account, 'id', 'Id');
          state.catalog = pick(data, 'catalog', 'Catalog') || [];
          renderCatalog();
          renderClaims(pick(data, 'warrantyClaims', 'WarrantyClaims'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', portalErrorMessage(root, err));
        });
    }

    root.addEventListener('click', function (e) {
      var review = e.target.closest('[data-ce-dealer-review]');
      if (review) {
        apiPost('/check-engine/dealer/TransitionWarrantyClaim', token, {
          claimId: Number(review.getAttribute('data-ce-dealer-review')),
          targetStatus: 1
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Transition failed.');
          });
      }
    });

    var orderBtn = root.querySelector('[data-ce-dealer-order]');
    if (orderBtn) {
      orderBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        var lines = [];
        root.querySelectorAll('[data-ce-dealer-qty]').forEach(function (input) {
          var qty = Number(input.value || 0);
          if (qty <= 0) return;
          var productId = Number(input.getAttribute('data-ce-dealer-qty'));
          var item = state.catalog.find(function (c) {
            return pick(c, 'productId', 'ProductId') === productId;
          });
          lines.push({
            productId: productId,
            quantity: qty,
            unitPrice: pick(item, 'dealerPrice', 'DealerPrice') || 0
          });
        });
        apiPost('/check-engine/dealer/PlaceDealerOrder', token, {
          dealerAccountId: state.accountId,
          lines: lines
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Order failed.');
          });
      });
    }

    var claimBtn = root.querySelector('[data-ce-dealer-claim]');
    if (claimBtn) {
      claimBtn.addEventListener('click', function () {
        if (!state.accountId) return;
        apiPost('/check-engine/dealer/SubmitWarrantyClaim', token, {
          dealerAccountId: state.accountId,
          oemNumber: root.querySelector('[data-ce-dealer-oem]')?.value || '',
          vehicleConfigurationId: Number(root.querySelector('[data-ce-dealer-vehicle]')?.value || 0)
        })
          .then(refresh)
          .catch(function (err) {
            showAlert(alert, 'error', (err && err.errorCode) || 'Claim failed.');
          });
      });
    }

    refresh();
  }

  function initPortalAdmin(root) {
    var token = root.querySelector('input[name="__RequestVerificationToken"]');
    var alert = root.querySelector('[data-ce-portal-alert]');
    function provision(path) {
      var customerId = Number(root.querySelector('[data-ce-admin-customer]')?.value || 0);
      var displayName = root.querySelector('[data-ce-admin-name]')?.value || '';
      apiPost(path, token, { customerId: customerId, displayName: displayName })
        .then(function (res) {
          var kind = pick(res, 'portalKind', 'PortalKind') || 'account';
          var accountId = pick(res, 'accountId', 'AccountId');
          showAlert(alert, 'success', 'Provisioned ' + kind + ' #' + accountId);
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Provision failed.');
        });
    }
    root.querySelector('[data-ce-admin-workshop]')?.addEventListener('click', function () {
      provision('/Admin/CheckEngine/PortalAdmin/ProvisionWorkshop');
    });
    root.querySelector('[data-ce-admin-fleet]')?.addEventListener('click', function () {
      provision('/Admin/CheckEngine/PortalAdmin/ProvisionFleet');
    });
    root.querySelector('[data-ce-admin-dealer]')?.addEventListener('click', function () {
      provision('/Admin/CheckEngine/PortalAdmin/ProvisionDealer');
    });
    root.querySelector('[data-ce-admin-seed-allocation]')?.addEventListener('click', function () {
      var dealerAccountId = Number(root.querySelector('[data-ce-admin-dealer-account]')?.value || 0);
      var productId = Number(root.querySelector('[data-ce-admin-product]')?.value || 0);
      var categoryId = Number(root.querySelector('[data-ce-admin-category]')?.value || 0);
      var periodCeilingUnits = Number(root.querySelector('[data-ce-admin-allocation-units]')?.value || 0);
      apiPost('/Admin/CheckEngine/PortalAdmin/SeedDealerAllocation', token, {
        dealerAccountId: dealerAccountId,
        productId: productId > 0 ? productId : null,
        categoryId: categoryId > 0 ? categoryId : null,
        periodCeilingUnits: periodCeilingUnits
      })
        .then(function (res) {
          showAlert(alert, 'success', 'Seeded allocation #' + pick(res, 'entityId', 'EntityId'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Seed allocation failed.');
        });
    });
    root.querySelector('[data-ce-admin-seed-quota]')?.addEventListener('click', function () {
      var dealerAccountId = Number(root.querySelector('[data-ce-admin-dealer-account]')?.value || 0);
      var spendCeiling = Number(root.querySelector('[data-ce-admin-spend-ceiling]')?.value || 0);
      apiPost('/Admin/CheckEngine/PortalAdmin/SeedDealerQuota', token, {
        dealerAccountId: dealerAccountId,
        spendCeiling: spendCeiling
      })
        .then(function (res) {
          showAlert(alert, 'success', 'Seeded quota #' + pick(res, 'entityId', 'EntityId'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Seed quota failed.');
        });
    });
    root.querySelector('[data-ce-admin-seed-franchise]')?.addEventListener('click', function () {
      apiPost('/Admin/CheckEngine/PortalAdmin/SeedDealerFranchise', token, {
        dealerAccountId: Number(root.querySelector('[data-ce-admin-franchise-account]')?.value || 0),
        makeId: Number(root.querySelector('[data-ce-admin-make-id]')?.value || 0),
        franchiseLabel: root.querySelector('[data-ce-admin-franchise-label]')?.value || ''
      })
        .then(function (res) {
          showAlert(alert, 'success', 'Seeded franchise #' + pick(res, 'entityId', 'EntityId'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Seed franchise failed.');
        });
    });
    root.querySelector('[data-ce-admin-seed-price]')?.addEventListener('click', function () {
      apiPost('/Admin/CheckEngine/PortalAdmin/SeedTradePriceListItem', token, {
        portalKind: root.querySelector('[data-ce-admin-portal-kind]')?.value || 'workshop',
        accountId: Number(root.querySelector('[data-ce-admin-trade-account]')?.value || 0),
        productId: Number(root.querySelector('[data-ce-admin-trade-product]')?.value || 0),
        unitPrice: Number(root.querySelector('[data-ce-admin-trade-price]')?.value || 0)
      })
        .then(function (res) {
          showAlert(alert, 'success', 'Price list #' + pick(res, 'entityId', 'EntityId'));
        })
        .catch(function (err) {
          showAlert(alert, 'error', (err && err.errorCode) || 'Seed price failed.');
        });
    });
  }

  function boot() {
    document.querySelectorAll('[data-ce-page="workshop"]').forEach(initWorkshop);
    document.querySelectorAll('[data-ce-page="fleet"]').forEach(initFleet);
    document.querySelectorAll('[data-ce-page="dealer"]').forEach(initDealer);
    document.querySelectorAll('[data-ce-page="portal-admin"]').forEach(initPortalAdmin);
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();

  window.CheckEnginePortals = { initWorkshop: initWorkshop, initFleet: initFleet, initDealer: initDealer };
})(window);
