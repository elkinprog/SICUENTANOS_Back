using Dominio.Administrador;
using Persistencia.Context;
using Dominio;
using System.Data;
using Microsoft.VisualBasic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Persistencia.Repository
{
    public class ParametroRepository : GenericRepository<Parametro>
    {

        public IGenericRepository<ParametroDetalle> _parametroDetalleRepository { get; }
        public  ApplicationDbContext _context;

        public ParametroRepository(ApplicationDbContext context, IGenericRepository<ParametroDetalle> parametroDetalleRepository) : base(context)
        {
            this._parametroDetalleRepository = parametroDetalleRepository;
            this._context = context;
        }



        public async Task<Boolean> ValidarExisteParametro(string codigoInterno)
        {
            return _context.Parametro.Any(p => p.VcCodigoInterno == codigoInterno);
        }




        public void insertMassiveData(ParametroRequest parametroRequest)
        {
            //insert to db
            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();

                using (SqlTransaction transaction = (SqlTransaction)connection.BeginTransaction())
                {
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, transaction))
                    {

                        try
                        { 
                            bulkCopy.DestinationTableName = "Parametro";
                            bulkCopy.WriteToServer(parametroRequest.Parametros);
                            
                            bulkCopy.DestinationTableName = "ParametroDetalle";
                            bulkCopy.WriteToServer(parametroRequest.ParametroDetalles);
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            connection.Close();
                            throw;
                        }

                    }
                }

            }

        }
    }
}
