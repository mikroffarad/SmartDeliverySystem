using AutoMapper;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ProductDto, Product>();

            CreateMap<Vendor, VendorWithProductsDto>();

            CreateMap<VendorDto, Vendor>();
            

            CreateMap<StoreDto, Store>();
        }
    }
}