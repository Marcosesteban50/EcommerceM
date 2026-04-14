using AutoMapper;
using EcommerceAPI.DTOs.CarritoDTOs;
using EcommerceAPI.DTOs.CategoriasDTOs;
using EcommerceAPI.DTOs.OrdenDTOs;
using EcommerceAPI.DTOs.ProductosDTOs;
using EcommerceAPI.DTOs.ProductosDTOs.EcommerceAPI.DTOs;
using EcommerceAPI.DTOs.UsuariosDTO;
using EcommerceAPI.Modelos;
using EcommerceAPI.Modelos.Enums;
using Microsoft.AspNetCore.Identity;

namespace EcommerceAPI.Utilidades
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            MapeoCategorias();
            MapeoProductos();
            MapeoUsuarios();
            MapeoCarritos();
            MapeoOrdenes();
        }


        private void MapeoCategorias()
        {
            CreateMap<CategoriaCreacionDTO, Categoria>();
            CreateMap<Categoria, CategoriaDTO>().
                ForMember(x => x.Productos, opc =>
                opc.MapFrom(src => src.Productos!.Where(p => p.Aprobado)));

            

        }

        private void MapeoProductos()
        {
            CreateMap<ProductoCreacionDTO, Producto>();
           


            CreateMap<ProductoHistorial, ProductoHistorialDTO>()
                .ForMember(x => x.CategoriaNombre, o => o.MapFrom(x => x.Categoria!.Nombre));

            CreateMap<Producto, ProductoDTO>()
                .ForMember(x => x.CategoriaNombre, o => o.MapFrom(x => x.Categoria!.Nombre));


            //Mapeamos hacia adelante y hacia atras
            CreateMap<Producto, AgregarMasProductosDTO>().ReverseMap();
            

            CreateMap<Producto, ProductoSinCategoriaDTO>();


        }

        private void MapeoOrdenes()
        {

            CreateMap<OrdenCreacionDTO,Orden>();
            CreateMap<Orden, OrdenListadoDTO>().ReverseMap();
            CreateMap<OrdenListadoDTO, Orden>();
            CreateMap<OrdenItems, OrdenItemDTO>().ReverseMap();
            CreateMap<EstadoOrdenCreacionDTO, EstadoOrden>();
            CreateMap<EstadoPagoCreacionDTO, EstadoPago>();
            CreateMap<Orden, EstadoOrdenDTO>().ReverseMap();
            CreateMap<Orden, EstadoPagoDTO>().ReverseMap();
            CreateMap<EstadoOrden,EstadoOrdenDTO>().ReverseMap();
             CreateMap<EstadoPago,EstadoPagoDTO>().ReverseMap();



        }


        private void MapeoUsuarios()
        {
            CreateMap<IdentityUser, UsuarioDTO>().ReverseMap();
            CreateMap<PerfilUsuario, PerfilUsuarioDTO>().ReverseMap();
        }


        private void MapeoCarritos()
        {
            CreateMap<Carrito, CarritoDTO>();

            CreateMap<CarritoItems, CarritoItemsDTO>()
                .ForMember(dest => dest.NombreProducto,
                           opt => opt.MapFrom(src => src.Producto.Nombre))
                .ForMember(dest => dest.Precio,
                           opt => opt.MapFrom(src => src.Producto.Precio))
            .ForMember(dest => dest.ImagenUrl, opt => opt.MapFrom(src => src.Producto.ImagenUrl));
        }
    }
}
