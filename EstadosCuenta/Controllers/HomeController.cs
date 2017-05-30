using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using EdoCta.Common;
using EdoCta.Tools;
using EstadosCuenta.Models;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace EstadosCuenta.Controllers
{
    public class HomeController : Controller
    {
        private async Task<List<string>> GetEmpresas()
        {
            string Empresas = ConfigurationManager.AppSettings["Empresas"];
            List<string> Empresa = new List<string>();
            try
            {
                if (!string.IsNullOrEmpty(Empresas))
                {
                    Empresa = Empresas.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }
            catch
            {
                Empresa = new List<string>();
            }
            return Empresa;
        }

        private async Task<Usuario> GetUsuario(string app, string empresa = null)
        {
            HttpCookie myCookie = null;
            JavaScriptSerializer js = null;
            Usuario usuario = new Usuario();
            bool flagPermisos = false;
            try
            {
                myCookie = Request.Cookies["UserSettingsEdoCta"];
                js = new JavaScriptSerializer();
                if (myCookie != null)
                {
                    if (!string.IsNullOrEmpty(myCookie["UserData"]))
                    {
                        try
                        {
                            usuario = (Usuario)js.Deserialize(myCookie["UserData"], typeof(Usuario));
                            usuario.Territorios = usuario.ObtenerTerritorios(empresa);
                            flagPermisos = usuario.Permisos != null ? (usuario.Permisos.Count > 0) : false;
                            if (!flagPermisos)
                            {
                                usuario = null;
                            }
                        }
                        catch
                        {
                            usuario = null;
                        }
                    }
                    else
                    {
                        usuario = null;
                    }
                }
                else
                {
                    try
                    {
                        myCookie = new HttpCookie("UserSettingsEdoCta");
                        usuario.Nombre = User.Identity.Name.Substring(User.Identity.Name.LastIndexOf(@"\") + 1);
                        usuario.Permisos = usuario.ObtenerPermisos(app);
                        usuario.Pass = usuario.ObtenerPassword();
                        usuario.Territorios = usuario.ObtenerTerritorios(empresa);
                        flagPermisos = usuario.Permisos != null ? (usuario.Permisos.Count > 0) : false;
                        if (flagPermisos)
                        {
                            myCookie["UserData"] = js.Serialize(usuario);
                            myCookie.Expires = DateTime.Now.AddYears(1000);
                        }
                        else
                        {
                            usuario = null;
                            myCookie = null;
                        }
                    }
                    catch
                    {
                        myCookie = null;
                        usuario = null;
                    }
                    if (myCookie != null)
                    {
                        Response.Cookies.Add(myCookie);
                    }
                }
                return usuario;
            }
            finally
            {
                js = null;
                usuario = null;
                myCookie = null;
            }
        }

        private async Task<ReportDocument> CargarRpt(long logId, string empresa)
        {
            ReportDocument crRpt = null;
            string reportPath = string.Empty;
            try
            {
                crRpt = new ReportDocument();
                reportPath = Server.MapPath("~/EstadosDeCuentaTemplates/" + empresa + "/" + ConfigurationManager.AppSettings["EstadoDeCuenta"]);
                crRpt.Load(reportPath);
                TableLogOnInfo logoninfo = new TableLogOnInfo();
                foreach (CrystalDecisions.CrystalReports.Engine.Table t in crRpt.Database.Tables)
                {
                    logoninfo = t.LogOnInfo;
                    logoninfo.ReportName = crRpt.Name;
                    logoninfo.ConnectionInfo.ServerName = ConfigurationManager.AppSettings[empresa + ".SAP.Server"];
                    logoninfo.ConnectionInfo.DatabaseName = ConfigurationManager.AppSettings[empresa + ".SAP.Database"];
                    logoninfo.ConnectionInfo.UserID = ConfigurationManager.AppSettings[empresa + ".SAP.User"];
                    logoninfo.ConnectionInfo.Password = ConfigurationManager.AppSettings[empresa + ".SAP.Password"];
                    logoninfo.TableName = t.Name;
                    t.ApplyLogOnInfo(logoninfo);
                    t.Location = t.Name;
                }
                PageMargins margins;
                // Get the PageMargins structure and set the 
                // margins for the report.
                margins = crRpt.PrintOptions.PageMargins;
                margins.bottomMargin = 150;
                margins.leftMargin = 150;
                margins.rightMargin = 150;
                margins.topMargin = 150;
                // Apply the page margins.
                crRpt.PrintOptions.ApplyPageMargins(margins);
                return crRpt;
            }
            catch (Exception ex)
            {
                LogTools.RegisterLog(logId, "", "", MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, string.Format("{0} exception caught here.", ex.GetType().ToString()), DateTime.Now);
                LogTools.RegisterLog(logId, "", "", MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex.Message, DateTime.Now);
                return null;
            }
            finally
            {
                reportPath = null;
            }
        }

        private async Task<Stream> GenerateEstadoDeCuentaPDF(long logId, Cliente cliente, ReportDocument crRpt)
        {
            try
            {
                crRpt.SetParameterValue("Cliente@", cliente.CardCode);
                return crRpt.ExportToStream(ExportFormatType.PortableDocFormat);
            }
            catch (Exception ex)
            {
                LogTools.RegisterLog(logId, "", "", MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, string.Format("{0} exception caught here.", ex.GetType().ToString()), DateTime.Now);
                LogTools.RegisterLog(logId, "", "", MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex.Message, DateTime.Now);
                return null;
            }
        }

        private async Task<bool> SendMail(long logId, Usuario usuario, string app, Cliente cliente, ReportDocument crRpt)
        {
            LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "Enviando Estado de cuenta a " + cliente.Email + " de cliente " + cliente.CardCode + " - " + cliente.CardName + ". ", DateTime.Now);

            string path = Server.MapPath("~/CorreoTemplates/" + cliente.Empresa + "/correo.html");
            string body = "";
            if (System.IO.File.Exists(path))
            {
                body = System.IO.File.ReadAllText(path);
            }
            else
            {
                return false;
            }
            Attachment attachment = null;

            //reemplazamos los textos
            body = body.Replace("@@EmpresaCorto", ConfigurationManager.AppSettings[cliente.Empresa + ".Empresa"].ToString())/*.Replace("@@Folio", cliente.CardName)*/;
            string title = "Estado de Cuenta @@EmpresaCorto - @@Folio";
            title = title.Replace("@@EmpresaCorto", cliente.Empresa).Replace("@@Folio", cliente.CardName);

            try
            {
                attachment = new Attachment(await GenerateEstadoDeCuentaPDF(logId, cliente, crRpt), cliente.Empresa + " " + cliente.CardName + ".pdf");
            }
            catch (Exception ex)
            {
                attachment = null;
                LogTools.RegisterLog(logId, "", "", MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex.Message, DateTime.Now);
            }

            if (ConfigurationManager.AppSettings[cliente.Empresa + ".MailTo"].ToString().Length > 0)
            {
                cliente.Email = ConfigurationManager.AppSettings[cliente.Empresa + ".MailTo"].ToString();
            }

            if (attachment != null)
            {
                return Correo.Enviar(cliente, usuario.Nombre, usuario.Pass, app, title, body, attachment, logId);
            }
            else
            {
                return false;
            }
        }

        private async Task<List<Cliente>> ProcesoCliente(bool upload, Cliente cliente, ReportDocument crRpt, long logId, Usuario usuario, string app, string email = null)
        {
            List<Cliente> clientes = new List<Cliente>();
            try
            {
                if (cliente != null)
                {
                    string territory = cliente.TerritoryIn;
                    try
                    {
                        if (upload)
                        {
                            if (territory == "*")
                            {
                                cliente.Usuario = usuario.Nombre;
                            }
                            cliente.TerritoryIn = string.Empty;
                        }
                        if (cliente.CargarDatos())
                        {
                            if (!upload)
                            {
                                territory = usuario.ObtenerTerritoriosComoCadena();
                                if (territory == "*")
                                {
                                    cliente.Usuario = string.Empty;
                                    territory = string.Empty;
                                }
                                else
                                {
                                    cliente.Usuario = usuario.Nombre;
                                }
                            }
                            if (string.IsNullOrEmpty(territory))
                            {
                                if (!string.IsNullOrEmpty(email))
                                {
                                    cliente.Email = email;
                                }
                                if (crRpt != null)
                                {
                                    cliente.Processed = await SendMail(logId, usuario, app, cliente, crRpt);
                                }
                                else
                                {
                                    cliente.Processed = false;
                                }
                                cliente.GuardarCambios();
                                clientes.Add(cliente);
                            }
                            else
                            {
                                if (territory.Replace("'", "").Replace(" ", "").Split(',').Contains(cliente.Territory))
                                {
                                    if (!string.IsNullOrEmpty(email))
                                    {
                                        cliente.Email = email;
                                    }
                                    if (crRpt != null)
                                    {
                                        cliente.Processed = await SendMail(logId, usuario, app, cliente, crRpt);
                                    }
                                    else
                                    {
                                        cliente.Processed = false;
                                    }
                                    cliente.GuardarCambios();
                                    clientes.Add(cliente);
                                }
                                else
                                {
                                    LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "[" + cliente.Empresa + "] --> El cliente " + cliente.CardName + "(" + cliente.CardCode + ") no se encuentra en el(los) territorio(s) " + territory + "; se encuentra en el territorio " + cliente.Territory + ".", DateTime.Now);
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        territory = null;
                    }
                }
                return clientes;
            }
            finally
            {
                clientes = null;
            }
        }

        public FileResult Ayuda()
        {
            string NombreArchivo = Server.MapPath("~/Ayuda/Manual usuario.pdf");
            var cd = new System.Net.Mime.ContentDisposition
            {
                // for example foo.bak
                FileName = "Manual de usuario.pdf",
                // always prompt the user for downloading, set to true if you want 
                // the browser to try to show the file inline
                Inline = false,
            };
            Response.AppendHeader("Content-Disposition", cd.ToString());
            return File(NombreArchivo, "application/pdf");
            //return null;
        }

        public FileResult ArchivoEjemplo()
        {
            string NombreArchivo = Server.MapPath("~/Ayuda/Ejemplo.xlsx");
            var cd = new System.Net.Mime.ContentDisposition
            {
                // for example foo.bak
                FileName = "Ejemplo.xlsx",
                // always prompt the user for downloading, set to true if you want 
                // the browser to try to show the file inline
                Inline = false,
            };
            Response.AppendHeader("Content-Disposition", cd.ToString());
            return File(NombreArchivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        public async Task<ActionResult> Index()
        {
            List<string> Empresa = null;
            Usuario usuario = null;
            string app = ConfigurationManager.AppSettings["App.Name"];
            try
            {
                Empresa = await GetEmpresas();
                usuario = await GetUsuario(app);
                if (usuario != null)
                {
                    ViewBag.UserName = usuario.Nombre;
                    return View(Empresa);
                }
                else
                {
                    return View("ErrorPermisos");
                }
            }
            finally
            {
                Empresa = null;
                usuario = null;
            }
        }

        public async Task<ActionResult> Inicio(string empresa)
        {
            Usuario usuario = null;
            string app = ConfigurationManager.AppSettings["App.Name"];
            try
            {
                usuario = await GetUsuario(app, empresa);
                if (!string.IsNullOrEmpty(empresa) && usuario != null)
                {
                    ViewBag.Empresa = empresa;
                    ViewBag.Empresas = await GetEmpresas();
                    ViewBag.UserName = usuario.Nombre;
                    return View();
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            finally
            {
                usuario = null;
            }
        }

        [HttpPost]
        public async Task<PartialViewResult> Upload()
        {
            long logId = 0;
            string app = ConfigurationManager.AppSettings["App.Name"];
            string Sbo = string.Empty;
            string Empresa = Request.Params["Empresa"];
            Usuario usuario = await GetUsuario(app, Empresa);
            logId = LogTools.RegisterLog(0, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "Procesando Archivo.", DateTime.Now);
            if (usuario != null)
            {
                if (usuario.Territorios != null)
                {
                    if (usuario.Territorios.Count > 0)
                    {
                        HttpPostedFileBase file = null;
                        int? fileSize = null;
                        string fileName = string.Empty;
                        string mimeType = string.Empty;
                        Workbook workbook = null;
                        Worksheet sheet = null;
                        DataTable data = null;
                        Cliente cliente = null;
                        List<Cliente> clientes = new List<Cliente>();
                        ReportDocument crRpt = null;
                        string territory = string.Empty;
                        try
                        {
                            Sbo = ConfigurationManager.AppSettings[Empresa + ".SBO"];
                            for (int i = 0; i < Request.Files.Count; i++)
                            {
                                file = Request.Files[i]; //Uploaded file
                                //Use the following properties to get file's name, size and MIMEType
                                fileSize = file.ContentLength;
                                fileName = file.FileName;
                                mimeType = file.ContentType;
                                workbook = new Workbook();
                                //Load a file and imports its data
                                workbook.LoadFromStream(file.InputStream);
                                //Initialize worksheet
                                sheet = workbook.Worksheets[0];
                                // get the data source that the grid is displaying data for
                                data = sheet.ExportDataTable();
                                sheet.Dispose();
                                workbook.Dispose();
                                if (data != null)
                                {
                                    crRpt = await CargarRpt(logId, Empresa);
                                    territory = usuario.ObtenerTerritoriosComoCadena();
                                    foreach (DataRow row in data.Rows)
                                    {
                                        cliente = new Cliente()
                                        {
                                            CardCode = row[0].ToString(),
                                            Empresa = Empresa,
                                            Sbo = Sbo,
                                            Usuario = !territory.Equals("*") ? usuario.Nombre : string.Empty,
                                            TerritoryIn = territory
                                        };
                                        if (!cliente.ExisteEnBaseDatos())
                                        {
                                            clientes.AddRange(await ProcesoCliente(true, cliente, crRpt, logId, usuario, app));
                                            /*if (usuario.Territorios.Any(v => v.Territory == "*"))
                                            {
                                                cliente.Usuario = usuario.Nombre;
                                            }
                                            cliente.TerritoryIn = string.Empty;
                                            if (cliente.CargarDatos())
                                            {
                                                if (string.IsNullOrEmpty(territory))
                                                {
                                                    if (crRpt != null)
                                                    {
                                                        cliente.Processed = await SendMail(logId, usuario, app, cliente, crRpt);
                                                    }
                                                    else
                                                    {
                                                        cliente.Processed = false;
                                                    }
                                                    cliente.GuardarCambios();
                                                    clientes.Add(cliente);
                                                }
                                                else
                                                {
                                                    if (territory.Replace("'", "").Replace(" ", "").Split(',').Contains(cliente.Territory))
                                                    {
                                                        if (crRpt != null)
                                                        {
                                                            cliente.Processed = await SendMail(logId, usuario, app, cliente, crRpt);
                                                        }
                                                        else
                                                        {
                                                            cliente.Processed = false;
                                                        }
                                                        cliente.GuardarCambios();
                                                        clientes.Add(cliente);
                                                    }
                                                    else
                                                    {
                                                        LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "El cliente " + cliente.CardName + "(" + cliente.CardCode + ") no se encuentra en el(los) territorio(s) " + territory + "; se encuentra en el territorio " + cliente.Territory + ".", DateTime.Now);
                                                    }
                                                }
                                            }*/
                                        }
                                    }
                                }
                            }
                            return PartialView("Grid", clientes);
                        }
                        catch (Exception ex)
                        {
                            LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "Error al procesar la información: " + ex.Message + ".", DateTime.Now);
                            return PartialView("Grid");
                        }
                        finally
                        {
                            file = null;
                            fileSize = null;
                            fileName = null;
                            mimeType = null;
                            workbook = null;
                            sheet = null;
                            data = null;
                            cliente = null;
                            Empresa = null;
                            clientes = null;
                            crRpt = null;
                        }
                    }
                    else
                    {
                        LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "El usuario no cuenta con territorios asignados.", DateTime.Now);
                        return PartialView("Grid");
                    }
                }
                else
                {
                    LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "El usuario no cuenta con territorios asignados.", DateTime.Now);
                    return PartialView("Grid");
                }
            }
            else
            {
                LogTools.RegisterLog(logId, "", app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "No se han cargado los datos del usuario para poder procesar la carga de datos.", DateTime.Now);
                return PartialView("Grid");
            }
        }

        [HttpPost]
        public async Task<PartialViewResult> Load(Cliente cliente, DateTime? inicio, DateTime? fin)
        {
            if (!inicio.HasValue)
            {
                inicio = DateTime.Today;
            }
            if (!fin.HasValue)
            {
                fin = DateTime.Today;
            }
            else
            {
                fin = fin.Value.AddDays(1);
            }
            long logId = 0;
            string app = ConfigurationManager.AppSettings["App.Name"];
            Usuario usuario = await GetUsuario(app, cliente.Empresa);
            logId = LogTools.RegisterLog(0, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "Cargando Clientes Procesados.", DateTime.Now);
            if (usuario != null)
            {
                List<Cliente> clientes = new List<Cliente>();
                try
                {
                    if (!string.IsNullOrEmpty(cliente.Empresa))
                    {
                        cliente.Sbo = ConfigurationManager.AppSettings[cliente.Empresa + ".SBO"];
                    }
                    if (usuario.Permisos != null)
                    {
                        if (!usuario.Permisos.Any(v => v.Nombre == "Todo"))
                        {
                            cliente.Usuario = usuario.Nombre;
                        }
                    }
                    else
                    {
                        cliente.Usuario = usuario.Nombre;
                    }
                    clientes = Cliente.GetClients(cliente, inicio.Value, fin.Value);
                    return PartialView("Grid", clientes);
                }
                catch (Exception ex)
                {
                    LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "Error al cargar la información: " + ex.Message + ".", DateTime.Now);
                    return PartialView("Grid");
                }
                finally
                {
                    clientes = null;
                }
            }
            else
            {
                LogTools.RegisterLog(logId, "", app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "No se han cargado los datos del usuario para poder mostrar los datos.", DateTime.Now);
                return PartialView("Grid");
            }
        }

        [HttpPost]
        public async Task<JsonResult> Procesar(Cliente cliente, string email)
        {
            long logId = 0;
            string app = ConfigurationManager.AppSettings["App.Name"];
            Usuario usuario = await GetUsuario(app, cliente.Empresa);
            ReportDocument crRpt = null;
            string territory = string.Empty;
            List<Cliente> clientes = null;
            logId = LogTools.RegisterLog(0, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "Procesando información del cliente.", DateTime.Now);
            if (usuario != null)
            {
                if (usuario.Territorios != null)
                {
                    if (usuario.Territorios.Count > 0)
                    {
                        try
                        {
                            crRpt = await CargarRpt(logId, cliente.Empresa);
                            if (!string.IsNullOrEmpty(cliente.Empresa))
                            {
                                cliente.Sbo = ConfigurationManager.AppSettings[cliente.Empresa + ".SBO"];
                            }
                            clientes = await ProcesoCliente(false, cliente, crRpt, logId, usuario, app, email);
                            if (clientes != null)
                            {
                                if (clientes.Count == 1)
                                {
                                    if (clientes.ElementAt(0).Processed)
                                    {
                                        return Json(new { HasError = false, Message = "Se ha procesado la información." });
                                    }
                                    else
                                    {
                                        return Json(new { HasError = true, Message = "El cliente " + cliente.CardName + "(" + cliente.CardCode + ") no se encuentra en el(los) territorio(s) " + territory + "; se encuentra en el territorio " + cliente.Territory + "." });
                                    }
                                }
                                else
                                {
                                    return Json(new { HasError = true, Message = "No ha sido posible procesar la información." });
                                }
                            }
                            else
                            {
                                return Json(new { HasError = true, Message = "No ha sido posible procesar la información." });
                            }
                            /*if (cliente.CargarDatos())
                            {
                                territory = usuario.ObtenerTerritoriosComoCadena();
                                if (territory == "*")
                                {
                                    cliente.Usuario = string.Empty;
                                    territory = string.Empty;
                                }
                                else
                                {
                                    cliente.Usuario = usuario.Nombre;
                                }
                                if (string.IsNullOrEmpty(territory))
                                {
                                    if (!string.IsNullOrEmpty(email))
                                    {
                                        cliente.Email = email;
                                    }
                                    if (crRpt != null)
                                    {
                                        cliente.Processed = await SendMail(logId, usuario, app, cliente, crRpt);
                                    }
                                    else
                                    {
                                        cliente.Processed = false;
                                    }
                                    cliente.GuardarCambios();
                                    return Json(new { HasError = false, Message = "Se ha procesado la información." });
                                }
                                else
                                {
                                    if (territory.Replace("'", "").Replace(" ", "").Split(',').Contains(cliente.Territory))
                                    {
                                        if (!string.IsNullOrEmpty(email))
                                        {
                                            cliente.Email = email;
                                        }
                                        if (crRpt != null)
                                        {
                                            cliente.Processed = await SendMail(logId, usuario, app, cliente, crRpt);
                                        }
                                        else
                                        {
                                            cliente.Processed = false;
                                        }
                                        cliente.GuardarCambios();
                                        return Json(new { HasError = false, Message = "Se ha procesado la información." });
                                    }
                                    else
                                    {
                                        LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "El cliente " + cliente.CardName + "(" + cliente.CardCode + ") no se encuentra en el(los) territorio(s) " + territory + "; se encuentra en el territorio " + cliente.Territory + ".", DateTime.Now);
                                        return Json(new { HasError = true, Message = "El cliente " + cliente.CardName + "(" + cliente.CardCode + ") no se encuentra en el(los) territorio(s) " + territory + "; se encuentra en el territorio " + cliente.Territory + "." });
                                    }
                                }
                            }*/
                        }
                        catch (Exception ex)
                        {
                            LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "Error al procesar la información: " + ex.Message + ".", DateTime.Now);
                            return Json(new { HasError = true, Message = "Error al procesar la información: " + ex.Message + "." });
                        }
                        finally
                        {
                            cliente = null;
                            crRpt = null;
                        }
                    }
                    else
                    {
                        LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "No se han cargado los datos del usuario para poder procesar la información.", DateTime.Now);
                        return Json(new { HasError = true, Message = "No se han cargado los datos del usuario para poder procesar la información." });
                    }
                }
                else
                {
                    LogTools.RegisterLog(logId, usuario.Nombre, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "No se han cargado los datos del usuario para poder procesar la información.", DateTime.Now);
                    return Json(new { HasError = true, Message = "No se han cargado los datos del usuario para poder procesar la información." });
                }
            }
            else
            {
                LogTools.RegisterLog(logId, "", app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "No se han cargado los datos del usuario para poder procesar la información.", DateTime.Now);
                return Json(new { HasError = true, Message = "No se han cargado los datos del usuario para poder procesar la información." });
            }
        }
    }
}