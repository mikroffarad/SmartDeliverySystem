using AutoMapper;
using SmartDeliverySystem.DTOs;
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Vendor mappings
            CreateMap<Vendor, VendorDto>().ReverseMap();
            CreateMap<Vendor, VendorWithProductsDto>()
                .ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.Products));

            // Store mappings
            CreateMap<Store, StoreDto>().ReverseMap();

            // Product mappings
            CreateMap<Product, ProductDto>().ReverseMap();
        }
    }
}
