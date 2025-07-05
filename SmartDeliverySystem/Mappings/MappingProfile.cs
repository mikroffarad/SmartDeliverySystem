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
            CreateMap<StoreDto, Store>(); CreateMap<AssignDriverDto, Delivery>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => DeliveryStatus.Assigned))
                .ForMember(dest => dest.AssignedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
