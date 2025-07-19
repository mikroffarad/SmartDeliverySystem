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

            // Delivery mappings
            CreateMap<Delivery, DeliveryDto>()
                .ForMember(dest => dest.DeliveryId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VendorName, opt => opt.MapFrom(src => src.Vendor.Name))
                .ForMember(dest => dest.StoreName, opt => opt.MapFrom(src => src.Store.Name))
                .ForMember(dest => dest.StoreLatitude, opt => opt.MapFrom(src => src.ToLatitude))
                .ForMember(dest => dest.StoreLongitude, opt => opt.MapFrom(src => src.ToLongitude))
                .ForMember(dest => dest.VendorLatitude, opt => opt.MapFrom(src => src.FromLatitude))
                .ForMember(dest => dest.VendorLongitude, opt => opt.MapFrom(src => src.FromLongitude));
            CreateMap<Delivery, DeliveryTrackingDto>()
                .ForMember(dest => dest.DeliveryId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.VendorLatitude, opt => opt.MapFrom(src => src.FromLatitude))
                .ForMember(dest => dest.VendorLongitude, opt => opt.MapFrom(src => src.FromLongitude))
                .ForMember(dest => dest.StoreLatitude, opt => opt.MapFrom(src => src.ToLatitude))
                .ForMember(dest => dest.StoreLongitude, opt => opt.MapFrom(src => src.ToLongitude));

            // Location update mappings
            CreateMap<LocationUpdateDto, LocationUpdateServiceBusDto>()
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.DeliveryId, opt => opt.Ignore());
        }
    }
}
