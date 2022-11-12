using Aplicacion.AgregarExcel;
using Aplicacion.ManejadorErrores;
using Aplicacion.Services;
using Dominio.Administrador;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using WebApi.Responses;

namespace WebApi.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class ParametroController : ControllerBase
    {
     
        private readonly IAgregarExcel _agregarExcel;

        private readonly IGenericService<Parametro> _service;
        private readonly IGenericService<ParametroDetalle> _serviceDetalle;
        public ParametroController(IGenericService<Parametro> service, IGenericService<ParametroDetalle> serviceDetalle, IAgregarExcel agregarExcel)
        {
            this._service = service;
            _serviceDetalle = serviceDetalle;
            this._agregarExcel = agregarExcel;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Parametro>>> GetParametro()
        {
            if (!_service.ExistsAsync())
            {
                throw new ExcepcionError(HttpStatusCode.NoContent, "ok", "No se encontraron Parametros en Base de Datos");
            }
            return await _service.GetAsync();
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<Parametro>> GetParametro(long Id)
        {
            if (!_service.ExistsAsync())
            {
                throw new ExcepcionError(HttpStatusCode.NotFound, "No Existe", "No se encontraron Parametros con este Id");
            }

            var Parametro = await _service.GetAsync(e => e.Id == Id, e => e.OrderBy(e => e.Id), "");

            if (Parametro.Count < 1)
            {
                throw new ExcepcionError(HttpStatusCode.NotFound, "No Existe", "No se encontraron Parametros con este Id");
            }
            return Created("ObtenerParametro", new { Codigo = HttpStatusCode.OK, Titulo = "OK", Mensaje = "Se obtuvo Id Solicitado", Parametro = Parametro });
        }

        [HttpPost]
        public async Task<IActionResult> PostParametro(Parametro parametro)
        {
            if (!_service.ExistsAsync())
            {
                throw new ExcepcionError(HttpStatusCode.NotFound, "No Modificado", "No fue posible Modificar Activivad");
            }

            parametro.DtFechaCreacion = DateTime.Now;
            parametro.DtFechaActualizacion = DateTime.Now;
            bool updated = await _service.CreateAsync(parametro);

            if (!updated)
            {
                throw new ExcepcionError(HttpStatusCode.NotModified, "No Modificado", "No se pudo Actualizar el Parametro");
            }
            return Created("ActualizarParametro", new { Codigo = HttpStatusCode.OK, Titulo = "OK", Mensaje = "Se Actualizo con Exito el Parametro" });
        }

        [HttpPut("{Id}")]
        public async Task<IActionResult> PutParametro(long Id, Parametro parametro)
        {
            if (Id != parametro.Id)
            {
                throw new ExcepcionError(HttpStatusCode.NotFound, "No Existe", "No fue posible encontrar el Id del Parametro");
            }
            bool updated = await _service.UpdateAsync(Id, parametro);

            if (!updated)
            {
                throw new ExcepcionError(HttpStatusCode.NotModified, "No Modificado", "No se pudo Actualizar el Parametro");
            }
            return Created("ActualizarParametro", new { Codigo = HttpStatusCode.OK, Titulo = "OK", Mensaje = "Se Actualizo con Exito el Parametro" });
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteParametro(long Id)
        {
            var listaParametroDetalle = await _serviceDetalle.GetAsync(e => e.ParametroId == Id);

            if (listaParametroDetalle.Any())
                return Created("EliminarParametro", new { Codigo = HttpStatusCode.NotModified, Titulo = "No valido", Mensaje = "Este parametro contiene registros que no se pueden eliminar." });

            var Parametro = await _service.GetAsync(e => e.Id == Id, e => e.OrderBy(e => e.Id), "");

            if (Parametro.Count < 1)
            {
                throw new ExcepcionError(HttpStatusCode.NotFound, "No Existe", "No se encontraron Parametros con este Id");
            }
            await _service.DeleteAsync(Id);

            return Created("EliminarParametro", new { Codigo = HttpStatusCode.OK, Titulo = "OK", Mensaje = "Se Elimino con Exito el Parametro" });
        }


        [HttpPost("cargar")]
        public async Task<ActionResult> Cargar([FromForm] List<IFormFile> files)
        {
            var horaInicio = DateTime.Now;
            var statusOk = HttpStatusCode.OK;
            var response = new CargaParametroResponse(statusOk, "Bien Hecho!", "datos cargados", null, 0);
            try
            {
                var errores = new List<string>();
                var registros = 0;

                if (files.Count == 0)
                {
                    response = new CargaParametroResponse(
                        HttpStatusCode.BadRequest,
                        "Algo salio mal",
                        "No se recibio el archivo ",
                        errores,
                        registros
                    );
                }

                string rutaInicial = Environment.CurrentDirectory;
                var nombreArchivo = files[0].FileName;  //parametrossicuentanos.xlsx

                var archivoArray = nombreArchivo.Split(".");
                var extencion = archivoArray[archivoArray.Count() - 1];

                if (extencion != "xlsx")
                {
                    response = new CargaParametroResponse(
                        HttpStatusCode.BadRequest,
                        "Algo salio mal",
                        "El Archivo no contiene el formato Excel ",
                        errores,
                        registros
                    );
                }
                else
                {
                   
                    DateTime now = DateTime.Now;
                    var horaNombre = now.ToString("yyyy-MM-dd-HH-mm-ss");
                    var rutaArchivo = rutaInicial + "/Upload/" + horaNombre + nombreArchivo;

                    if (files.Count == 1)
                    {
                        System.IO.File.Delete(rutaArchivo);
                    }

                    using (var str = System.IO.File.Create(rutaArchivo))
                    {
                        str.Position = 0;
                        await files[0].CopyToAsync(str);
                    }

                    var responseParametro = _agregarExcel.procesarArchivo(rutaArchivo);
                  

                    if (responseParametro.Errores.Count() > 0)
                    {
                        response = new CargaParametroResponse(
                            HttpStatusCode.BadRequest,
                            "Datos Vacios en Documento Excel",
                            "Falta un valor en alguna celda del archivo excel",
                            responseParametro.Errores,
                            registros
                        );
                    }
                    else if (responseParametro.Registros == 0)
                    {
                        response = new CargaParametroResponse(
                            statusOk,
                            "Archivo sin procesar",
                            "No se procesó el archivo debido a que ya existían los parámetros en base de datos",
                            errores,
                            registros
                        );
                    }
                    else
                    {
                        response = new CargaParametroResponse(
                            statusOk,
                            "Parametros cargados", "Se cargaron (" + responseParametro.Registros + ") parametros y ParametrosDetalle, archivo: " + nombreArchivo,
                            responseParametro.Errores,
                            responseParametro.Registros
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                response = new CargaParametroResponse(
                    HttpStatusCode.BadRequest,
                    "Algo salio mal",
                    "Error procesando el archivo " + ex.Message + " " + ex.StackTrace,
                    null,
                    0
               );
            }
            var horaFin = DateTime.Now;
            var tiempo = horaFin - horaInicio;
            response.Mensaje += " Petición resulta en " + tiempo.ToString();
            return StatusCode((int)response.Codigo, response);
        }
    }
}





