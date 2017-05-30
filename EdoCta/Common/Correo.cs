using System;
using System.Configuration;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;

namespace EdoCta.Common
{
    public class Correo
    {
        public static bool Enviar(Cliente cliente, string usuario, string pass, string app, string subject, string body, Attachment attachment, long logId)
        {
            Tools.LogTools.RegisterLog(logId, usuario, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "sending mail...", DateTime.Now);

            try
            {
                //validamos datos
                if (cliente.Email == null)
                {
                    throw new Exception("Email de destinatario no puede estar vacío.");
                }
                string mailUser = ConfigurationManager.AppSettings["MailUser"].ToString();
                if (string.IsNullOrEmpty(mailUser))
                {
                    if (string.IsNullOrEmpty(usuario))
                    {
                        throw new Exception("El nombre de usuario no puede estar vacío.");
                    }
                    usuario = usuario.Contains("@") ? usuario : usuario + "@hiteconline.net";
                    mailUser = usuario;
                }
                string mailPass = ConfigurationManager.AppSettings["MailPass"].ToString();
                if (string.IsNullOrEmpty(mailPass))
                {
                    if (string.IsNullOrEmpty(pass))
                    {
                        throw new Exception("La contreaseña de usuario no puede estar vacía.");
                    }
                    mailPass = pass;
                }
                string mailFrom = ConfigurationManager.AppSettings["MailFrom"].ToString();
                if (string.IsNullOrEmpty(mailFrom))
                {
                    if (string.IsNullOrEmpty(usuario))
                    {
                        throw new Exception("El nombre de usuario no puede estar vacío.");
                    }
                    mailFrom = usuario;
                }
                //creamos el objeto mail
                SmtpClient client = new SmtpClient(ConfigurationManager.AppSettings["MailSMTP"].ToString());
                client.EnableSsl = true;
                client.Credentials = new System.Net.NetworkCredential(mailUser, mailPass);
                MailMessage message = new MailMessage();

                //llenamos el remitente y el destinatario
                message.From = new MailAddress(mailFrom);

                foreach (string mailDir in cliente.Email.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.To.Add(mailDir.Trim());
                }
                foreach (string mailDir in ConfigurationManager.AppSettings[cliente.Empresa + ".MailBcc"].ToString().Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.Bcc.Add(mailDir.Trim());
                }
                if (!string.IsNullOrEmpty(mailUser))
                {
                    if (mailUser.Contains("@hiteconline.net"))
                    {
                        message.Bcc.Add(mailUser.Replace("@hiteconline.net", "@grupohitec.com"));
                    }
                    else
                    {
                        message.Bcc.Add(mailUser);
                    }
                }
                //agregamos archivos adjuntos
                if (attachment != null)
                {
                    message.Attachments.Add(attachment);
                }

                //configuramos el mensaje
                message.Body = body;
                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.IsBodyHtml = true;
                message.Subject = subject;
                message.SubjectEncoding = System.Text.Encoding.UTF8;

                //enviamos el mail
                client.TargetName = "STARTTLS/smtp.office365.com";
                client.Send(message);
                Tools.LogTools.RegisterLog(logId, usuario, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, "Mail Sent...", DateTime.Now);
                return true;
            }
            catch (Exception ex)
            {
                Tools.LogTools.RegisterLog(logId, usuario, app, MethodBase.GetCurrentMethod().DeclaringType.Name, MethodBase.GetCurrentMethod().Name, ex.Message, DateTime.Now);
            }
            return false;
        }
    }
}
