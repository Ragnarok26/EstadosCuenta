function AbrirModal(Modal) {
    if (Modal) {
        if (Modal.MostrarEncabezado) {
            if (Modal.MostrarEncabezado === true) {
                if ($("#EncabezadoModal").hasClass("hidden")) {
                    $("#EncabezadoModal").removeClass("hidden");
                }
            } else {
                if (!$("#EncabezadoModal").hasClass("hidden")) {
                    $("#EncabezadoModal").addClass("hidden");
                }
            }
            if (Modal.MostrarIconoCerrar) {
                if (Modal.MostrarIconoCerrar === true) {
                    if ($("#IconoCerrarModal").hasClass("hidden")) {
                        $("#IconoCerrarModal").removeClass("hidden")
                    }
                } else {
                    if (!$("#IconoCerrarModal").hasClass("hidden")) {
                        $("#IconoCerrarModal").addClass("hidden")
                    }
                }
            } else {
                if (!$("#IconoCerrarModal").hasClass("hidden")) {
                    $("#IconoCerrarModal").addClass("hidden")
                }
            }
            if (Modal.Titulo) {
                $("#TituloModal").html(Modal.Titulo);
            } else {
                $("#TituloModal").html("");
            }
        } else {
            if (!$("#EncabezadoModal").hasClass("hidden")) {
                $("#EncabezadoModal").addClass("hidden");
            }
        }
        if (Modal.MostrarPie) {
            if (Modal.MostrarPie === true) {
                if ($("#PieModal").hasClass("hidden")) {
                    $("#PieModal").removeClass("hidden");
                }
            } else {
                if (!$("#PieModal").hasClass("hidden")) {
                    $("#PieModal").addClass("hidden");
                }
            }
            if (Modal.MostrarBotonCerrar) {
                if (Modal.MostrarBotonCerrar === true) {
                    if ($("#BotonCerrarModal").hasClass("hidden")) {
                        $("#BotonCerrarModal").removeClass("hidden");
                    }
                } else {
                    if (!$("#BotonCerrarModal").hasClass("hidden")) {
                        $("#BotonCerrarModal").addClass("hidden");
                    }
                }
            } else {
                if (!$("#BotonCerrarModal").hasClass("hidden")) {
                    $("#BotonCerrarModal").addClass("hidden");
                }
            }
        } else {
            if (!$("#PieModal").hasClass("hidden")) {
                $("#PieModal").addClass("hidden");
            }
        }
        if (Modal.AlineacionMensaje) {
            $("#CuerpoModal").css("text-align", Modal.AlineacionMensaje);
        } else {
            $("#CuerpoModal").css("text-align", "");
        }
        if (Modal.Mensaje) {
            $("#CuerpoModal").html(Modal.Mensaje);
        } else {
            $("#CuerpoModal").html("");
        }
    } else {
        if (!$("#EncabezadoModal").hasClass("hidden")) {
            $("#EncabezadoModal").addClass("hidden");
        }
        if (!$("#IconoCerrarModal").hasClass("hidden")) {
            $("#IconoCerrarModal").addClass("hidden")
        }
        if (!$("#PieModal").hasClass("hidden")) {
            $("#PieModal").addClass("hidden");
        }
        $("#CuerpoModal").css("text-align", "center");
        $("#CuerpoModal").html("<p><h3>Cargando...</h3></p>");
    }
    var windowHeight = $(window).height();
    var windowWidth = $(window).width();
    var boxHeight = $('#LoadModal').height();
    var boxWidth = $('#LoadModal').width();
    $('#LoadModal').css({ 'left': ((windowWidth - boxWidth) / 2), 'top': ((windowHeight - boxHeight) / 2) });
    $("#LoadModal").modal("show");
}

function CerrarModal() {
    $("#LoadModal").modal("hide");
}