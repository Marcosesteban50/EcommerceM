using EcommerceAPI.Modelos;
using EcommerceAPI.Modelos.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EcommerceAPI.DTOs.OrdenDTOs
{
    public class OrdenCreacionDTO
    {

        public string EstadoOrdenId { get; set; } = null!;
        public string EstadoPagoId { get; set; } = null!;


        

    }
}
