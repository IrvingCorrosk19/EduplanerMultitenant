/**
 * User Password Management - list users, filter by role (client-side like User/Index), select multiple users.
 * Prepares for future Mass Password Delivery feature.
 */
var userPasswordManagement = (function () {
    'use strict';

    var config = {
        listUrl: '',
        sendPasswordsUrl: '',
        serverRendered: false,
        indexUrl: ''
    };

    var dataTable = null;
    var currentData = [];
    var ROLE_COLUMN_INDEX = 3; // Column "Role" (0=checkbox, 1=Name, 2=Email, 3=Role, 4=Status, 5=Created At)

    function formatDate(dateStr) {
        if (!dateStr) return '';
        var d = new Date(dateStr);
        if (isNaN(d.getTime())) return dateStr;
        return d.toLocaleDateString('es-PA', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function getRoleDisplayName(role) {
        if (!role) return '';
        var r = (role + '').toLowerCase();
        if (r === 'superadmin') return 'SuperAdmin';
        if (r === 'admin') return 'Admin';
        if (r === 'teacher') return 'Teacher';
        if (r === 'student' || r === 'estudiante') return 'Student';
        return role;
    }

    function getAllCheckboxes() {
        // Cuando DataTables paginea, en el DOM quedan solo las filas del page actual.
        // Para "Select all" real necesitamos consultar los nodos de TODAS las filas.
        if (dataTable && $.fn.DataTable.isDataTable('#usersTable')) {
            return dataTable.rows().nodes().to$().find('.user-select-cb');
        }
        return $('#usersTable').find('.user-select-cb');
    }

    function refreshSelectAllState() {
        var $selectAll = $('#selectAll');
        var $allCheckboxes = getAllCheckboxes();
        var total = $allCheckboxes.length;
        var checked = $allCheckboxes.filter(':checked').length;
        $selectAll.prop('checked', total > 0 && checked === total);
        $selectAll.prop('indeterminate', checked > 0 && checked < total);
    }

    function escapeRegex(str) {
        return (str + '').replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    /**
     * Load all users once (like User/Index); filtering is done client-side with DataTables.
     */
    function loadUsers() {
        return $.ajax({
            url: config.listUrl,
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            currentData = Array.isArray(data) ? data : [];
            renderTable(currentData);
        }).fail(function (xhr) {
            console.error('userPasswordManagement: loadUsers failed', xhr);
            currentData = [];
            renderTable([]);
        });
    }

    /**
     * Apply role filter using DataTables column search (client-side, like User/Index).
     */
    function applyRoleFilter() {
        if (!dataTable || !$.fn.DataTable.isDataTable('#usersTable')) return;
        var role = $('#role').val() || '';
        if (role === '') {
            dataTable.column(ROLE_COLUMN_INDEX).search('').draw();
        } else {
            dataTable.column(ROLE_COLUMN_INDEX).search('^' + escapeRegex(role) + '$', true, false).draw();
        }
    }

    /**
     * Clear all filters and redraw (client-side, like User/Index Reset).
     */
    function clearFilters() {
        $('#role').val('');
        $('#q').val('');
        if (dataTable && $.fn.DataTable.isDataTable('#usersTable')) {
            dataTable.column(ROLE_COLUMN_INDEX).search('');
            dataTable.search('');
            dataTable.draw();
        }
    }

    /**
     * Render the table with data and (re)initialize DataTables.
     */
    function renderTable(users) {
        var $table = $('#usersTable');
        var $tbody = $table.find('tbody');
        $tbody.empty();

        users.forEach(function (u) {
            var name = (u.firstName || '') + ' ' + (u.lastName || '').trim();
            if (!name.trim()) name = u.email || '';
            var row = '<tr data-id="' + (u.id || '') + '">' +
                '<td><input type="checkbox" class="user-select-cb" value="' + (u.id || '') + '" /></td>' +
                '<td>' + escapeHtml(name) + '</td>' +
                '<td>' + escapeHtml(u.email || '') + '</td>' +
                '<td>' + escapeHtml(getRoleDisplayName(u.role)) + '</td>' +
                '<td>' + escapeHtml(u.grade != null && u.grade !== '' ? u.grade : '-') + '</td>' +
                '<td>' + escapeHtml(u.group != null && u.group !== '' ? u.group : '-') + '</td>' +
                '<td>' + escapeHtml(u.status || '') + '</td>' +
                '<td>' + escapeHtml(u.passwordEmailStatus || '') + '</td>' +
                '<td>' + escapeHtml(formatDate(u.passwordEmailSentAt)) + '</td>' +
                '<td>' + escapeHtml(formatDate(u.createdAt)) + '</td>' +
                '</tr>';
            $tbody.append(row);
        });

        if (dataTable && $.fn.DataTable.isDataTable($table)) {
            dataTable.destroy();
            dataTable = null;
        }

        dataTable = $table.DataTable({
            pageLength: 25,
            order: [[1, 'asc']],
            columnDefs: [
                { orderable: false, targets: 0 }
            ],
            language: {
                search: 'Buscar:',
                lengthMenu: 'Mostrar _MENU_ registros',
                info: 'Mostrando _START_ a _END_ de _TOTAL_',
                infoEmpty: 'Sin registros',
                infoFiltered: '(filtrado de _MAX_)',
                paginate: {
                    first: '«',
                    last: '»',
                    next: '›',
                    previous: '‹'
                }
            }
        });

        handleCheckboxSelection();
    }

    function initDataTableOnExistingRows() {
        var $table = $('#usersTable');
        if (dataTable && $.fn.DataTable.isDataTable($table)) {
            dataTable.destroy();
            dataTable = null;
        }
        dataTable = $table.DataTable({
            pageLength: 25,
            order: [[1, 'asc']],
            searching: false,
            columnDefs: [
                { orderable: false, targets: 0 }
            ],
            language: {
                search: 'Buscar:',
                lengthMenu: 'Mostrar _MENU_ registros',
                info: 'Mostrando _START_ a _END_ de _TOTAL_',
                infoEmpty: 'Sin registros',
                infoFiltered: '(filtrado de _MAX_)',
                paginate: {
                    first: '«',
                    last: '»',
                    next: '›',
                    previous: '‹'
                }
            }
        });
        handleCheckboxSelection();
    }

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Handle "Select all" and per-row checkboxes; expose selected IDs for future use.
     */
    function handleCheckboxSelection() {
        var $table = $('#usersTable');
        var $selectAll = $('#selectAll');
        // Evitamos capturar una colección estática (DataTables cambia el DOM entre páginas).
        // Usamos delegación de eventos sobre la tabla.

        $selectAll.off('change').on('change', function () {
            var checked = $(this).prop('checked');
            getAllCheckboxes().prop('checked', checked);
            refreshSelectAllState();
        });

        $table.off('change.userPasswordManagement', '.user-select-cb')
            .on('change.userPasswordManagement', '.user-select-cb', function () {
                refreshSelectAllState();
            });

        refreshSelectAllState();
        if (dataTable) {
            dataTable.off('draw.userPasswordManagement');
            dataTable.on('draw.userPasswordManagement', function () {
                refreshSelectAllState();
            });
        }
    }

    /**
     * Get currently selected user IDs (for future Mass Password Delivery).
     */
    function getSelectedIds() {
        return getAllCheckboxes().filter(':checked').map(function () {
            return $(this).val();
        }).get();
    }

    function init(options) {
        if (options) {
            config.listUrl = options.listUrl || config.listUrl;
            config.sendPasswordsUrl = options.sendPasswordsUrl || config.sendPasswordsUrl;
            config.serverRendered = !!options.serverRendered;
            config.indexUrl = options.indexUrl || config.indexUrl;
        }

        $('#btnSendPasswords').on('click', function () {
            sendPasswords();
        });

        if (config.serverRendered) {
            initDataTableOnExistingRows();
            $('#btnReset').on('click', function (e) {
                e.preventDefault();
                var p = new URLSearchParams();
                var gi = $('#gradeId').val();
                var gr = $('#groupId').val();
                if (gi) { p.set('gradeId', gi); }
                if (gr) { p.set('groupId', gr); }
                var base = (config.indexUrl || '').split('?')[0];
                window.location.href = base + (p.toString() ? '?' + p.toString() : '');
            });
        } else {
            $('#roleFilter').on('change', function () {
                applyRoleFilter();
            });
            $('#btnReset').on('click', function () {
                clearFilters();
            });
            $('#searchBox').on('keyup', function () {
                if (dataTable && $.fn.DataTable.isDataTable('#usersTable')) {
                    dataTable.search(this.value).draw();
                }
            });
            loadUsers();
        }
    }

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val() || '';
    }

    var MAX_SEND = 2000;

    /**
     * Envío masivo: contraseña temporal por correo (Resend). Requiere usuarios seleccionados.
     */
    function sendPasswords() {
        var ids = getSelectedIds();
        if (!ids.length) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({ icon: 'warning', title: 'Selección', text: 'Seleccione al menos un usuario.' });
            } else { alert('Seleccione al menos un usuario.'); }
            return;
        }
        if (ids.length > MAX_SEND) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'warning',
                    title: 'Límite',
                    text: 'Máximo ' + MAX_SEND + ' usuarios por envío. Reduzca la selección.'
                });
            } else { alert('Máximo ' + MAX_SEND + ' usuarios por envío.'); }
            return;
        }
        if (!config.sendPasswordsUrl) {
            if (typeof Swal !== 'undefined') {
                Swal.fire({ icon: 'error', title: 'Error', text: 'URL de envío no configurada.' });
            } else { alert('URL de envío no configurada.'); }
            return;
        }

        var doPost = function () {
            var $btn = $('#btnSendPasswords');
            $btn.prop('disabled', true);

            // Loading explícito para que el usuario no vea "resultado" desaparecer
            // inmediatamente por el reload al servidor-render.
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'info',
                    title: 'Enviando...',
                    html: 'Generando contraseñas temporales y encolando correos.<br/><small class="text-muted">Esto puede tardar unos segundos.</small>',
                    allowOutsideClick: false,
                    allowEscapeKey: false,
                    showConfirmButton: false,
                    willOpen: function () {
                        Swal.showLoading();
                    }
                });
            }

            $.ajax({
                url: config.sendPasswordsUrl,
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify({ userIds: ids }),
                headers: {
                    RequestVerificationToken: getAntiForgeryToken()
                }
            }).done(function (res) {
                if (res && res.success) {
                    // Respuesta honesta: mostrar conteos reales y jobId
                    var html = escapeHtml(res.message || 'Correos encolados.');
                    if (res.acceptedCount > 0) {
                        html += '<br/><small class="text-muted">Encolados: <strong>' + res.acceptedCount + '</strong>';
                        if (res.rejectedCount > 0) {
                            html += ' · Omitidos: <strong>' + res.rejectedCount + '</strong>';
                        }
                        html += '</small>';
                    }
                    if (res.jobId) {
                        html += '<br/><small class="text-muted">Job ID: <code>' + escapeHtml(res.jobId) + '</code></small>';
                    }
                    if (typeof Swal !== 'undefined') {
                        // Mantener el mensaje visible hasta que el usuario lo cierre,
                        // y solo después recargar si aplica.
                        Swal.close();
                        Swal.fire({
                            icon: 'success',
                            title: 'Envío iniciado',
                            html: html,
                            allowOutsideClick: false,
                            allowEscapeKey: false,
                            confirmButtonText: 'OK'
                        }).then(function () {
                            if (config.serverRendered) {
                                window.location.reload();
                            } else {
                                loadUsers();
                            }
                        });
                    } else {
                        // Sin SweetAlert: fallback simple
                        alert(res.message);
                        if (config.serverRendered) {
                            window.location.reload();
                        } else {
                            loadUsers();
                        }
                    }
                } else {
                    // El servidor respondió pero con success=false (no es error HTTP)
                    var errMsg = res && res.message ? res.message : 'El sistema no pudo encolar los correos.';
                    var warnings = (res && res.warnings && res.warnings.length) ? res.warnings : [];
                    var errHtml = escapeHtml(errMsg);
                    if (warnings.length) {
                        errHtml += '<ul style="text-align:left;margin-top:8px;">';
                        warnings.forEach(function (w) { errHtml += '<li>' + escapeHtml(w) + '</li>'; });
                        errHtml += '</ul>';
                    }
                    if (typeof Swal !== 'undefined') {
                        Swal.close();
                        Swal.fire({ icon: 'warning', title: 'Sin correos encolados', html: errHtml, allowOutsideClick: false, allowEscapeKey: false, confirmButtonText: 'OK' });
                    } else { alert(errMsg); }
                }
            }).fail(function (xhr) {
                var errData = xhr.responseJSON || {};
                var msg = errData.message || xhr.statusText || 'Error de red.';
                    if (typeof Swal !== 'undefined') {
                        Swal.close();
                        Swal.fire({ icon: 'error', title: 'Error', text: msg, allowOutsideClick: false, allowEscapeKey: false, confirmButtonText: 'OK' });
                } else { alert('Error: ' + msg); }
            }).always(function () {
                $btn.prop('disabled', false);
            });
        };

        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'question',
                title: 'Enviar contraseñas',
                html: 'Se generará una <strong>contraseña temporal nueva</strong> por usuario, se guardará en el sistema y se enviará por correo en segundo plano.<br/><br/>Máximo ' + MAX_SEND + ' por solicitud.',
                showCancelButton: true,
                confirmButtonText: 'Sí, enviar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#0d6efd'
            }).then(function (r) {
                if (r.isConfirmed) doPost();
            });
        } else if (confirm('¿Generar contraseña temporal y enviar por correo a cada usuario seleccionado?')) {
            doPost();
        }
    }

    return {
        init: init,
        loadUsers: loadUsers,
        applyRoleFilter: applyRoleFilter,
        clearFilters: clearFilters,
        handleCheckboxSelection: handleCheckboxSelection,
        getSelectedIds: getSelectedIds,
        sendPasswords: sendPasswords
    };
})();
