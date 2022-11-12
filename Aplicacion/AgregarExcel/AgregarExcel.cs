using Dominio;
using Dominio.Administrador;
using SpreadsheetLight;
using Persistencia.Repository;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data;

namespace Aplicacion.AgregarExcel
{
    public interface IAgregarExcel
    {
        ParametroRequest parametro(string ruta);
        ParametroRequest parametrodetalle(string ruta, DataTable parametros, ref List<string> errores);
        ParametroRequest  procesarArchivo(string ruta);
    }

    public class AgregarExcel : IAgregarExcel
    {
        public ParametroRepository _parametroRepository { get;}

        public AgregarExcel(ParametroRepository parametroRepository)
        {
            this._parametroRepository = parametroRepository;
        }
         
        public ParametroRequest procesarArchivo(string ruta)
        {
            var response = parametro(ruta);
            var list    = response.Parametros;
            var errores = response.Errores;
            
            if (response.Errores.Count() == 0 && list.Rows.Count > 0) 
            {
                //Requiere de la lista de parametros
                var responseDetalle = parametrodetalle(ruta, list, ref errores);

                if (responseDetalle.Errores.Count() == 0)
                {
                    response.ParametroDetalles = responseDetalle.ParametroDetalles;
                    response.Errores = errores;
                    _parametroRepository.insertMassiveData(response);
                    response.Registros = list.Rows.Count + response.ParametroDetalles.Rows.Count;
                }
            }
            return response;
        }

        public ParametroRequest parametro(string ruta)
        {
            SLDocument sL = new SLDocument(ruta, "PARAMETRO");
            SLWorksheetStatistics stats = sL.GetWorksheetStatistics();
            int iStartColumnIndex = stats.StartColumnIndex;
            List<string>    errores = new List<string>();

            var param = new DataTable();
            param.Columns.Add("Id", typeof(long));
            param.Columns.Add("VcNombre", typeof(string));
            param.Columns.Add("VcCodigoInterno", typeof(string));
            param.Columns.Add("BEstado", typeof(bool));
            param.Columns.Add("DtFechaCreacion", typeof(DateTime));
            param.Columns.Add("DtFechaActualizacion", typeof(DateTime));
            param.Columns.Add("DtFechaAnulacion", typeof(DateTime));

            int row;

            try
            {
                for (row = stats.StartRowIndex + 1; row <= stats.EndRowIndex; ++row)
                {
                    var Id              = sL.GetCellValueAsInt64( row, iStartColumnIndex + 0);
                    var VcNombre        = sL.GetCellValueAsString(row, iStartColumnIndex + 1);
                    var VcCodigoInterno = sL.GetCellValueAsString(row, iStartColumnIndex + 2);
                    var BEstado         = sL.GetCellValueAsString(row, iStartColumnIndex + 3);
                   
                    if (Id <= 0)
                    {
                        errores.Add("El id del parámetro, no puede estar vacio o tener texto en la celda (A" + row.ToString() + ")");
                    }

                    if (String.IsNullOrEmpty(VcNombre))
                    {
                        errores.Add("El nombre del parámetro (" + VcNombre + "), no puede ser vacio en la celda (B" + row.ToString() + ")");
                    }
                    
                    if (VcNombre.Count()>100 )
                    {
                        errores.Add("El nombre parámetro (" + VcNombre + "), no puede ser mayor a 100 caracteres, en la celda (B" + row.ToString() + ")");
                    }

                    if (String.IsNullOrEmpty(VcCodigoInterno))
                    {
                        errores.Add("El nombre del código interno (" + VcCodigoInterno + "), este no puede ser vacio en la celda (C" + row.ToString() + ")");
                    }

                    if (VcCodigoInterno.Count() > 50)
                    {
                        errores.Add("El código interno (" + VcCodigoInterno + "), no puede ser mayor a 50 caracteres en la celda (C" + row.ToString() + ")");
                    }

                    if (String.IsNullOrEmpty(BEstado))
                    {
                        errores.Add("No se encontró el estado, este no puede ser vacio en la celda (D" + row.ToString() + ")");
                    }

                    if (!(BEstado.Equals("0") || BEstado.Equals("1"))) 
                    {
                        errores.Add("El estado debe tener valores 0 o 1, en la celda (D" + row.ToString() + ")");
                    }


                  
                    var validacion = _ = _parametroRepository.ValidarExisteParametro(VcCodigoInterno);


                    if (errores.Count() == 0  && !validacion.Result) 
                    {

                        param.Rows.Add(new object[]
                        {
                            Id,
                            VcNombre,
                            VcCodigoInterno,
                            BEstado == "1",
                            DateTime.Now,
                            DateTime.Now,
                            null
                        });
                    }
                }

               //sL.CloseWithoutSaving();

                return new ParametroRequest
                {
                    Parametros= param, 
                    Errores=errores,
                };
            }
            catch (Exception ex)
            { 
                throw;
            }
        }

        public ParametroRequest parametrodetalle(string ruta, DataTable parametros, ref List<string> errores)
        {
   
            SLDocument doc = new SLDocument(ruta, "PARAMETRO_DETALLE");
            SLWorksheetStatistics stats = doc.GetWorksheetStatistics();
            int StartColumnIndex = stats.StartColumnIndex;
            var paramdet = new DataTable();
            paramdet.Columns.Add("Id", typeof(long));
            paramdet.Columns.Add("ParametroId", typeof(long));
            paramdet.Columns.Add("VcNombre", typeof(string));
            paramdet.Columns.Add("TxDescripcion", typeof(string));
            paramdet.Columns.Add("VcCodigoInterno", typeof(string));
            paramdet.Columns.Add("DCodigoIterno", typeof(decimal));
            paramdet.Columns.Add("BEstado", typeof(Boolean));
            paramdet.Columns.Add("RangoDesde", typeof(int));
            paramdet.Columns.Add("RangoHasta", typeof(int));
            paramdet.Columns.Add("IdPadre", typeof(long));

            int row;

            try
            {
                for (row = stats.StartRowIndex + 1; row <= stats.EndRowIndex; ++row)
                {
                    // no se necesita en Objeto
                    var Id              = doc.GetCellValueAsInt64 (row, StartColumnIndex +0);
                    var ParametroId     = doc.GetCellValueAsInt64 (row, StartColumnIndex +1);
                    var VcNombre        = doc.GetCellValueAsString(row, StartColumnIndex +2);
                    var TxDescripcion   = doc.GetCellValueAsString(row, StartColumnIndex +3);
                    var IdPadre         = doc.GetCellValueAsString(row, StartColumnIndex +4);
                    var VcCodigoInterno = doc.GetCellValueAsString(row, StartColumnIndex +5);
                    var DCodigoIterno   = doc.GetCellValueAsString(row, StartColumnIndex +6);
                    var BEstado         = doc.GetCellValueAsString(row, StartColumnIndex +7);
                    var RangoDesde      = doc.GetCellValueAsInt32 (row, StartColumnIndex +8);
                    var RangoHasta      = doc.GetCellValueAsInt32 (row, StartColumnIndex +9);


                    if (Id <= 0)
                    {
                        errores.Add("El id del parámetrodetalle, no puede estar vacio o tener texto en la celda (A" + row.ToString() + ")");
                    }

                    if (ParametroId <= 0)
                    {
                        errores.Add("El parámetroid del parámetrodetalle, no puede estar vacio o tener texto en la celda (B" + row.ToString() + ")");
                    }

                    //validar que parametroId este en la lista de parametros
                    
                    var parametro = (from x in parametros.Rows.OfType<DataRow>()
                                 where x.Field<long>("Id") == ParametroId
                                 select x).ToList();
                    
                    //var parametro = parametros.Any(x => x.Id != ParametroId);
                    if (parametro.Count == 0)
                    {
                        errores.Add("El id(" + Id + "), de parámetro no coincide con la lista de parámetrodetalle (" + ParametroId + "),");
                    }

                    if (String.IsNullOrEmpty(VcNombre))
                    {
                        errores.Add("El nombre del parámetrodetalle (" + VcNombre + "), no puede ser vacio en la celda (C" + row.ToString() + ")");
                    }


                    if (!String.IsNullOrEmpty(IdPadre))
                    {
                        var parametrodetalle = (from x in paramdet.Rows.OfType<DataRow>()
                                         where x.Field<long>("Id") == long.Parse(IdPadre)
                                                select x).ToList();

                        //var parametrodetalle = parametrosDetalle.Any(x => x.Id == long.Parse(IdPadre));
                        if (parametrodetalle.Count == 0)
                        {
                            errores.Add("El IdPadre de parámetrodetalle no esta en la lista de parametros enviados  (F" + row.ToString() + ")");
                        }
                    }

                    if (!String.IsNullOrEmpty(DCodigoIterno))
                    {
                        if (!decimal.TryParse(DCodigoIterno,out _))
                        {
                            errores.Add("El codigoInterno  de parámetrodetalle debe ser un decimal (G" + row.ToString() + ")");
                        }
                    }

                    if (String.IsNullOrEmpty(BEstado))
                    {
                        errores.Add("El estado de parámetrodetalle no puede ser vacio en la celda (H" + row.ToString() + ")");
                    }


                    if (RangoHasta < RangoDesde)
                    {
                        errores.Add("El RangoHasta  de parámetrodetalle no puede ser menor al  RangoDesde  de parámetrodetalleen la celda (J" + row.ToString() + ")");
                    }


                    if (errores.Count() == 0)
                    {

                        paramdet.Rows.Add(new object[]
                        {
                            Id,
                            ParametroId,
                            VcNombre,
                            TxDescripcion,
                            VcCodigoInterno,
                            decimal.TryParse(DCodigoIterno, out decimal valor) ? valor : null,
                            BEstado == "1",
                            RangoDesde,
                            RangoHasta,
                            long.TryParse(IdPadre,out long numero) ?  numero : null,

                        });
                    }

                }
                    //doc.CloseWithoutSaving();

                    return new ParametroRequest
                    {
                        ParametroDetalles = paramdet,
                        Errores = errores,
                    };
                
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }


}
        







