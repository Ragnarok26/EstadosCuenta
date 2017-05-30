$(document).ready(
    function () {
        $("#clsForm").on('click',
            function () {
                $("#fileInputLbl").get(0).firstChild.nodeValue = "Selecciona un Archivo...";
            }
        );

        $("#fileInput").on('change',
            function () {
                var data = new FormData();
                var fileInput = $(this)[0];
                if (fileInput.files.length > 0) {
                    for (i = 0; i < fileInput.files.length; i++) {
                        $("#" + $(this).prop('id') + "Lbl").get(0).firstChild.nodeValue = String(fileInput.files[i].name);
                    }
                } else {
                    $("#fileInputLbl").get(0).firstChild.nodeValue = "Selecciona un Archivo...";
                }
            }
        );

        $('#uploader').on('submit',
            function (e) {
                e.preventDefault();
                AbrirModal(
                    {
                        Mensaje: "<p><h3>Cargando...</h3></p>",
                        AlineacionMensaje: "center"
                    }
                );
                var contador = 0;
                var data = new FormData(); //FormData object
                var fileInput = $('#fileInput')[0];
                //Iterating through each files selected in fileInput
                for (i = 0; i < fileInput.files.length; i++) {
                    //Appending each file to FormData object
                    data.append(fileInput.files[i].name, fileInput.files[i]);
                    contador++;
                }
                data.append("Empresa", $("#txtEmpresa").val());
                if (contador > 0) {
                    //Creating an XMLHttpRequest and sending
                    $.ajax(
                        {
                            dataType: "html",
                            type: "POST",
                            url: url_Upload,
                            contentType: false,
                            processData: false,
                            data: data,
                            success: function (data, textStatus, jqXHR) {
                                Load(false);
                            },
                            error: function (jqXHR, textStatus, errorThrown) {
                                $.toast(
                                    {
                                        text: errorThrown + "\n(" + JSON.stringify(jqXHR, null, 4) + ")", // Text that is to be shown in the toast
                                        heading: textStatus, // Optional heading to be shown on the toast
                                        icon: 'error', // Type of toast icon
                                        showHideTransition: 'fade', // fade, slide or plain
                                        allowToastClose: true, // Boolean value true or false
                                        hideAfter: 6000, // false to make it sticky or number representing the miliseconds as time after which toast needs to be hidden
                                        stack: 5, // false if there should be only one toast at a time or a number representing the maximum number of toasts to be shown at a time
                                        position: 'bottom-right', // bottom-left or bottom-right or bottom-center or top-left or top-right or top-center or mid-center or an object representing the left, right, top, bottom values
                                        textAlign: 'center',  // Text alignment i.e. left, right or center
                                        loader: false // Whether to show loader or not. True by default
                                    }
                                );
                                CerrarModal();
                            }
                        }
                    );
                } else {
                    $.toast(
                        {
                            text: "Favor de Seleccionar un Archivo.", // Text that is to be shown in the toast
                            heading: "Advertencia", // Optional heading to be shown on the toast
                            icon: 'warning', // Type of toast icon
                            showHideTransition: 'fade', // fade, slide or plain
                            allowToastClose: true, // Boolean value true or false
                            hideAfter: 6000, // false to make it sticky or number representing the miliseconds as time after which toast needs to be hidden
                            stack: 5, // false if there should be only one toast at a time or a number representing the maximum number of toasts to be shown at a time
                            position: 'bottom-right', // bottom-left or bottom-right or bottom-center or top-left or top-right or top-center or mid-center or an object representing the left, right, top, bottom values
                            textAlign: 'center',  // Text alignment i.e. left, right or center
                            loader: false // Whether to show loader or not. True by default
                        }
                    );
                    CerrarModal();
                }
            }
        );

        $("#btnFiltro").on('click',
            function () {
                Load(true);
            }
        );

        $('#filters-container .input-daterange').datepicker(
            {
                format: "dd/mm/yyyy",
                todayBtn: "linked",
                language: "es",
                autoclose: true
            }
        );

        Load(true);
    }
);

function Load(mostrarModal) {
    if (mostrarModal) {
        if (mostrarModal === true) {
            AbrirModal(
                {
                    Mensaje: "<p><h3>Cargando...</h3></p>",
                    AlineacionMensaje: "center"
                }
            );
        }
    }
    $.ajax(
        {
            dataType: "html",
            type: "POST",
            url: url_Load,
            data: { "cliente": { "Empresa": $("#txtEmpresa").val() }, "inicio": $("#txtInicio").val(), "fin": $("#txtFin").val() },
            success: function (data, textStatus, jqXHR) {
                $("#divLoad").html(data);
                $("#divLoad > div > table > tbody > tr > td > input.procesar").click(
                    function () {
                        Procesar($(this).attr('id').replace("C_", ""));
                    }
                );
                CerrarModal();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                $.toast(
                    {
                        text: errorThrown + "\n(" + JSON.stringify(jqXHR, null, 4) + ")", // Text that is to be shown in the toast
                        heading: textStatus, // Optional heading to be shown on the toast
                        icon: 'error', // Type of toast icon
                        showHideTransition: 'fade', // fade, slide or plain
                        allowToastClose: true, // Boolean value true or false
                        hideAfter: 6000, // false to make it sticky or number representing the miliseconds as time after which toast needs to be hidden
                        stack: 5, // false if there should be only one toast at a time or a number representing the maximum number of toasts to be shown at a time
                        position: 'bottom-right', // bottom-left or bottom-right or bottom-center or top-left or top-right or top-center or mid-center or an object representing the left, right, top, bottom values
                        textAlign: 'center',  // Text alignment i.e. left, right or center
                        loader: false // Whether to show loader or not. True by default
                    }
                );
                CerrarModal();
            }
        }
    );
}

function Procesar(IdCliente) {
    AbrirModal(
        {
            Mensaje: "<p><h3>Procesando...</h3></p>",
            AlineacionMensaje: "center"
        }
    );
    $.ajax(
        {
            dataType: "json",
            type: "POST",
            url: url_Operacion,
            data: { "cliente": { "Id": IdCliente, "Cardcode": $("#IC_" + IdCliente).val(), "Empresa": $("#txtEmpresa").val() }, "email": $("#C_" + IdCliente).val() },
            success: function (data, textStatus, jqXHR) {
                if (data != null) {
                    $.toast(
                        {
                            text: data.Message, // Text that is to be shown in the toast
                            heading: data.HasError === false ? 'Exitoso' : 'Error', // Optional heading to be shown on the toast
                            icon: data.HasError === false ? 'success' : 'error', // Type of toast icon
                            showHideTransition: 'fade', // fade, slide or plain
                            allowToastClose: true, // Boolean value true or false
                            hideAfter: 6000, // false to make it sticky or number representing the miliseconds as time after which toast needs to be hidden
                            stack: 5, // false if there should be only one toast at a time or a number representing the maximum number of toasts to be shown at a time
                            position: 'bottom-right', // bottom-left or bottom-right or bottom-center or top-left or top-right or top-center or mid-center or an object representing the left, right, top, bottom values
                            textAlign: 'center',  // Text alignment i.e. left, right or center
                            loader: false // Whether to show loader or not. True by default
                        }
                    );
                    Load(false);
                } else {
                    $.toast(
                        {
                            text: "Error al intentar realizar la operacion.", // Text that is to be shown in the toast
                            heading: "Error", // Optional heading to be shown on the toast
                            icon: 'error', // Type of toast icon
                            showHideTransition: 'fade', // fade, slide or plain
                            allowToastClose: true, // Boolean value true or false
                            hideAfter: 6000, // false to make it sticky or number representing the miliseconds as time after which toast needs to be hidden
                            stack: 5, // false if there should be only one toast at a time or a number representing the maximum number of toasts to be shown at a time
                            position: 'bottom-right', // bottom-left or bottom-right or bottom-center or top-left or top-right or top-center or mid-center or an object representing the left, right, top, bottom values
                            textAlign: 'center',  // Text alignment i.e. left, right or center
                            loader: false // Whether to show loader or not. True by default
                        }
                    );
                }
                CerrarModal();
            },
            error: function (jqXHR, textStatus, errorThrown) {
                $.toast(
                    {
                        text: errorThrown + "\n(" + JSON.stringify(jqXHR, null, 4) + ")", // Text that is to be shown in the toast
                        heading: textStatus, // Optional heading to be shown on the toast
                        icon: 'error', // Type of toast icon
                        showHideTransition: 'fade', // fade, slide or plain
                        allowToastClose: true, // Boolean value true or false
                        hideAfter: 6000, // false to make it sticky or number representing the miliseconds as time after which toast needs to be hidden
                        stack: 5, // false if there should be only one toast at a time or a number representing the maximum number of toasts to be shown at a time
                        position: 'bottom-right', // bottom-left or bottom-right or bottom-center or top-left or top-right or top-center or mid-center or an object representing the left, right, top, bottom values
                        textAlign: 'center',  // Text alignment i.e. left, right or center
                        loader: false // Whether to show loader or not. True by default
                    }
                );
                CerrarModal();
            }
        }
    );
}