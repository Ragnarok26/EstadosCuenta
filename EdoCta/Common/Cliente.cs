using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;

namespace EdoCta.Common
{
    public class Cliente
    {
        public long Id { get; set; }
        [Display(Name = "No. Cliente")]
        public string CardCode { get; set; }
        [Display(Name = "Nombre")]
        public string CardName { get; set; }
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }
        [Display(Name = "Estado")]
        public bool Processed { get; set; }
        [Display(Name = "Fecha de Creación")]
        public DateTime Date { get; set; }
        [Display(Name = "Fecha Última Actualización")]
        public DateTime Update { get; set; }
        [Display(Name = "Empresa")]
        public string Empresa { get; set; }
        [Display(Name = "Activo")]
        public bool Status { get; set; }
        [Display(Name = "Territorio")]
        public string Territory { get; set; }
        [Display(Name = "TerritorioID")]
        public int? TerritoryId { get; set; }
        public string TerritoryIdIn { get; set; }
        public string TerritoryIn { get; set; }
        public bool Search { get; set; }
        public bool Found { get; set; }
        public string Sbo { get; set; }
        public string Usuario { get; set; }

        public bool CargarDatos()
        {
            List<string> columnName = null;
            PropertyInfo propertyInfo = null;
            columnName = new List<string>();
            dynamic value = null;
            Type dataType = null;
            try
            {
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[this.Empresa + ".SAPDB"].ConnectionString))
                {
                    conn.Open();
                    using (System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("SELECT c.CardCode, c.CardName, c.E_Mail as Email, GETDATE() as Date, 1 as Found, CASE c.frozenFor WHEN 'Y' THEN 0 ELSE 1 END AS Status, c.Territory as TerritoryId, t.descript as Territory FROM " + this.Sbo + ".dbo.OCRD c INNER JOIN " + this.Sbo + ".dbo.OTER t on c.Territory=t.territryID WHERE c.CardType = 'C'" + (!string.IsNullOrEmpty(this.TerritoryIn) ? " AND t.descript IN(" + this.TerritoryIn + ")" : "") + (!string.IsNullOrEmpty(this.TerritoryIdIn) ? " AND c.Territory IN(" + this.TerritoryIdIn + ")" : ""), conn))
                    {
                        if (!string.IsNullOrEmpty(this.CardCode))
                        {
                            command.CommandText += " AND c.CardCode = @CardCode";
                            command.Parameters.Add("CardCode", SqlDbType.NVarChar).Value = this.CardCode;
                        }
                        if (!string.IsNullOrEmpty(this.Territory))
                        {
                            command.CommandText += " AND t.descript = @Territory";
                            command.Parameters.Add("Territory", SqlDbType.NVarChar).Value = this.Territory;
                        }
                        if (this.TerritoryId.HasValue)
                        {
                            command.CommandText += " AND c.Territory = @TerritoryId";
                            command.Parameters.Add("TerritoryId", SqlDbType.Int).Value = this.TerritoryId;
                        }
                        using (System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            for (int x = 0; x < reader.FieldCount; x++)
                            {
                                columnName.Add(reader.GetName(x));
                            }
                            while (reader.Read())
                            {
                                for (int x = 0; x < columnName.Count; x++)
                                {
                                    propertyInfo = this.GetType().GetProperty(columnName[x]);
                                    if (propertyInfo != null)
                                    {
                                        dataType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                                        value = (reader[columnName[x]] == null ? null : (reader[columnName[x]] == DBNull.Value ? null : Convert.ChangeType(reader[columnName[x]], dataType)));
                                        propertyInfo.SetValue(this, value, null);
                                        propertyInfo = null;
                                    }
                                }
                            }
                            reader.Close();
                        }
                    }
                    conn.Close();
                }
                return this.Found;
            }
            catch
            {
                return false;
            }
            finally
            {
                columnName = null;
                propertyInfo = null;
                columnName = null;
                value = null;
                dataType = null;
            }
        }

        public bool ExisteEnBaseDatos()
        {
            string territory = this.TerritoryIn;
            if (!string.IsNullOrEmpty(this.TerritoryIn))
            {
                if (this.TerritoryIn.Trim().Equals("*"))
                {
                    this.TerritoryIn = string.Empty;
                }
            }
            List<Cliente> clientes = null;
            try
            {
                clientes = Cliente.GetClientsFromDatabase(this, DateTime.Today, DateTime.Today.AddDays(1).AddSeconds(-1));
                this.TerritoryIn = territory;
                return clientes != null ? clientes.Count > 0 : false;
            }
            finally
            {
                clientes = null;
                territory = null;
            }
        }

        public bool GuardarCambios()
        {
            int? rowsAffected = null;
            try
            {
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["HaasCnc.EdoCtaDB"].ConnectionString))
                {
                    conn.Open();
                    using (System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(this.Id > 0 ? @"UPDATE ec_CustomerToProcess 
                            SET MailAddress = @MailAddress, ErrorMessage = @ErrorMessage, Processed = @Processed, Active = @Active, UpdateDate = GETDATE(), Empresa = @Empresa, [User] = @User, Territory = @Territory, TerritoryId = @TerritoryId
                            WHERE Id = @Id;" : 
                            /*@"INSERT INTO ec_CustomerToProcess(SapCustomerId, MailAddress, ErrorMessage, Processed, Active, CreateDate, UpdateDate, Empresa)
                            VALUES(@SapCustomerId, @MailAddress, @ErrorMessage, @Processed, @Active, GETDATE(), GETDATE(), @Empresa);"*/
                            @"IF NOT EXISTS(SELECT Id FROM ec_CustomerToProcess WHERE SapCustomerId = @SapCustomerId AND CONVERT(DATE, CreateDate) = CONVERT(DATE, GETDATE()) AND [User] = @User AND Territory = @Territory AND TerritoryId = @TerritoryId)
                              BEGIN
	                            INSERT INTO ec_CustomerToProcess(SapCustomerId, MailAddress, ErrorMessage, Processed, Active, CreateDate, UpdateDate, Empresa, [User], Territory, TerritoryId)
	                            VALUES(@SapCustomerId, @MailAddress, @ErrorMessage, @Processed, @Active, GETDATE(), GETDATE(), @Empresa, @User, @Territory, @TerritoryId);
                              END
                              ELSE
                              BEGIN
	                            UPDATE ec_CustomerToProcess 
                                SET MailAddress = @MailAddress, ErrorMessage = @ErrorMessage, Processed = @Processed, Active = @Active, UpdateDate = GETDATE(), Empresa = @Empresa
                                WHERE SapCustomerId = @SapCustomerId AND CONVERT(DATE, CreateDate) = CONVERT(DATE, GETDATE()) AND [User] = @User AND Territory = @Territory AND TerritoryId = @TerritoryId;
                              END", conn))
                    {
                        if (this.Id > 0)
                        {
                            command.Parameters.Add("Id", SqlDbType.BigInt).Value = this.Id;
                        }
                        if (!string.IsNullOrEmpty(this.CardCode))
                        {
                            command.Parameters.Add("SapCustomerId", SqlDbType.NVarChar).Value = this.CardCode;
                        }
                        if (!string.IsNullOrEmpty(this.Email))
                        {
                            command.Parameters.Add("MailAddress", SqlDbType.NVarChar).Value = this.Email;
                        }
                        if (!string.IsNullOrEmpty(this.Usuario))
                        {
                            command.Parameters.Add("User", SqlDbType.NVarChar).Value = this.Usuario;
                        }
                        if (!string.IsNullOrEmpty(this.Territory))
                        {
                            command.Parameters.Add("Territory", SqlDbType.NVarChar).Value = this.Territory;
                        }
                        if (this.TerritoryId.HasValue)
                        {
                            command.Parameters.Add("TerritoryId", SqlDbType.Int).Value = this.TerritoryId;
                        }
                        command.Parameters.Add("ErrorMessage", SqlDbType.NVarChar).Value = DBNull.Value;
                        command.Parameters.Add("Processed", SqlDbType.Bit).Value = this.Processed;
                        command.Parameters.Add("Active", SqlDbType.Bit).Value = this.Status;
                        command.Parameters.Add("Empresa", SqlDbType.NVarChar).Value = this.Empresa;
                        rowsAffected = command.ExecuteNonQuery();
                    }
                    conn.Close();
                }
                return rowsAffected.HasValue ? rowsAffected.Value > 0 : rowsAffected.HasValue;
            }
            catch
            {
                return false;
            }
            finally
            {
                rowsAffected = null;
            }
        }

        public static List<Cliente> GetClients(Cliente cliente, DateTime inicio, DateTime fin)
        {
            List<Cliente> clientes = GetClientsFromDatabase(cliente, inicio, fin);
            if (clientes != null)
            {
                for (int x = 0; x < clientes.Count; x++)
                {
                    clientes[x].Empresa = cliente.Empresa;
                    clientes[x] = GetClientDataFromSAP(clientes[x]);
                }
                return clientes;
            }
            return new List<Cliente>();
        }

        private static List<Cliente> GetClientsFromDatabase(Cliente cliente, DateTime inicio, DateTime fin)
        {
            Type listType = null;
            dynamic dataObjects = null;
            List<string> columnName = null;
            dynamic element = null;
            PropertyInfo propertyInfo = null;
            columnName = new List<string>();
            dynamic value = null;
            Type dataType = null;
            try
            {
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["HaasCnc.EdoCtaDB"].ConnectionString))
                {
                    conn.Open();
                    using (System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("SELECT Id, SapCustomerId AS CardCode, MailAddress AS Email, Processed, Active AS Status, CreateDate AS Date, UpdateDate AS 'Update', [User] AS Usuario, Territory, TerritoryId FROM ec_CustomerToProcess WHERE (CreateDate BETWEEN @inicio AND @fin)" + (!string.IsNullOrEmpty(cliente.TerritoryIn) ? " AND Territory IN(" + cliente.TerritoryIn + ")" : "") + (!string.IsNullOrEmpty(cliente.TerritoryIdIn) ? " AND TerritoryId IN(" + cliente.TerritoryIdIn + ")" : ""), conn))
                    {
                        command.Parameters.Add("inicio", SqlDbType.DateTime).Value = inicio;
                        command.Parameters.Add("fin", SqlDbType.DateTime).Value = fin;
                        if (cliente != null)
                        {
                            if (!string.IsNullOrEmpty(cliente.Empresa))
                            {
                                command.CommandText += " AND Empresa = @Empresa";
                                command.Parameters.Add("Empresa", SqlDbType.NVarChar).Value = cliente.Empresa;
                            }
                            if (!string.IsNullOrEmpty(cliente.CardCode))
                            {
                                command.CommandText += " AND SapCustomerId = @SapCustomerId";
                                command.Parameters.Add("SapCustomerId", SqlDbType.NVarChar).Value = cliente.CardCode;
                            }
                            if (!string.IsNullOrEmpty(cliente.Usuario))
                            {
                                command.CommandText += " AND [User] = @User";
                                command.Parameters.Add("User", SqlDbType.NVarChar).Value = cliente.Usuario;
                            }
                            if (!string.IsNullOrEmpty(cliente.Territory))
                            {
                                command.CommandText += " AND Territory = @Territory";
                                command.Parameters.Add("Territory", SqlDbType.NVarChar).Value = cliente.Territory;
                            }
                            if (cliente.TerritoryId.HasValue)
                            {
                                command.CommandText += " AND TerritoryId = @TerritoryId";
                                command.Parameters.Add("TerritoryId", SqlDbType.Int).Value = cliente.TerritoryId;
                            }
                        }
                        using (System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            for (int x = 0; x < reader.FieldCount; x++)
                            {
                                columnName.Add(reader.GetName(x));
                            }
                            listType = typeof(List<>).MakeGenericType(new Type[] { typeof(Cliente) });
                            dataObjects = Activator.CreateInstance(listType);
                            while (reader.Read())
                            {
                                element = Activator.CreateInstance(typeof(Cliente));
                                for (int x = 0; x < columnName.Count; x++)
                                {
                                    propertyInfo = element.GetType().GetProperty(columnName[x]);
                                    dataType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                                    value = (reader[columnName[x]] == null ? null : (reader[columnName[x]] == DBNull.Value ? null : Convert.ChangeType(reader[columnName[x]], dataType)));
                                    propertyInfo.SetValue(element, value, null);
                                    propertyInfo = null;
                                }
                                dataObjects.Add(element);
                                element = null;
                            }
                            reader.Close();
                        }
                    }
                    conn.Close();
                }
                return dataObjects;
            }
            catch
            {
                return new List<Cliente>();
            }
            finally
            {
                listType = null;
                dataObjects = null;
                columnName = null;
                element = null;
                propertyInfo = null;
                columnName = null;
                value = null;
                dataType = null;
            }
        }

        private static Cliente GetClientDataFromSAP(Cliente cliente)
        {
            List<string> columnName = null;
            PropertyInfo propertyInfo = null;
            columnName = new List<string>();
            dynamic value = null;
            Type dataType = null;
            try
            {
                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[cliente.Empresa + ".SAPDB"].ConnectionString))
                {
                    conn.Open();
                    using (System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand("SELECT c.CardCode, c.CardName,/* c.E_Mail as Email,*/ GETDATE() as Date, 1 as Found, CASE c.frozenFor WHEN 'Y' THEN 0 ELSE 1 END AS Status, c.Territory as TerritoryId, t.descript as Territory FROM " + cliente.Sbo + ".dbo.OCRD c INNER JOIN " + cliente.Sbo + ".dbo.OTER t on c.Territory=t.territryID WHERE c.CardType = 'C'" + (!string.IsNullOrEmpty(cliente.TerritoryIn) ? " AND t.descript IN(" + cliente.TerritoryIn + ")" : "") + (!string.IsNullOrEmpty(cliente.TerritoryIdIn) ? " AND c.Territory IN(" + cliente.TerritoryIdIn + ")" : ""), conn))
                    {
                        if (!string.IsNullOrEmpty(cliente.CardCode))
                        {
                            command.CommandText += " AND c.CardCode = @CardCode";
                            command.Parameters.Add("CardCode", SqlDbType.NVarChar).Value = cliente.CardCode;
                        }
                        if (!string.IsNullOrEmpty(cliente.Territory))
                        {
                            command.CommandText += " AND t.descript = @Territory";
                            command.Parameters.Add("Territory", SqlDbType.NVarChar).Value = cliente.Territory;
                        }
                        if (cliente.TerritoryId.HasValue)
                        {
                            command.CommandText += " AND c.Territory = @TerritoryId";
                            command.Parameters.Add("TerritoryId", SqlDbType.Int).Value = cliente.TerritoryId;
                        }
                        using (System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            for (int x = 0; x < reader.FieldCount; x++)
                            {
                                columnName.Add(reader.GetName(x));
                            }
                            while (reader.Read())
                            {
                                for (int x = 0; x < columnName.Count; x++)
                                {
                                    propertyInfo = cliente.GetType().GetProperty(columnName[x]);
                                    if (propertyInfo != null)
                                    {
                                        dataType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                                        value = (reader[columnName[x]] == null ? null : (reader[columnName[x]] == DBNull.Value ? null : Convert.ChangeType(reader[columnName[x]], dataType)));
                                        propertyInfo.SetValue(cliente, value, null);
                                        propertyInfo = null;
                                    }
                                }
                            }
                            reader.Close();
                        }
                    }
                    conn.Close();
                }
                return cliente;
            }
            catch
            {
                return new Cliente();
            }
            finally
            {
                columnName = null;
                propertyInfo = null;
                columnName = null;
                value = null;
                dataType = null;
            }
        }
    }
}