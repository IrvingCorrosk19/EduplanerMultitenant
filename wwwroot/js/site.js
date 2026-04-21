// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Función para mostrar mensajes de error
function showError(message) {
    Swal.fire({
        icon: 'error',
        title: 'Error',
        text: message,
        confirmButtonText: 'Aceptar'
    });
}

// Función para mostrar mensajes de éxito
function showSuccess(message) {
    Swal.fire({
        icon: 'success',
        title: 'Éxito',
        text: message,
        confirmButtonText: 'Aceptar'
    });
}

// Función para mostrar mensajes de confirmación
function showConfirm(message, callback) {
    Swal.fire({
        title: '¿Está seguro?',
        text: message,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sí',
        cancelButtonText: 'No'
    }).then((result) => {
        if (result.isConfirmed) {
            callback();
        }
    });
}

// Función para mostrar mensajes de información
function showInfo(message) {
    Swal.fire({
        icon: 'info',
        title: 'Información',
        text: message,
        confirmButtonText: 'Aceptar'
    });
}

// Función para mostrar mensajes de advertencia
function showWarning(message) {
    Swal.fire({
        icon: 'warning',
        title: 'Advertencia',
        text: message,
        confirmButtonText: 'Aceptar'
    });
}
