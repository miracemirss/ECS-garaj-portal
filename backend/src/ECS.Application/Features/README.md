# Application Features — Dikey Dilim (Vertical Slice) Deseni

Her feature aynı yapıyı izler. Örnek: `Vehicles/`

```
Vehicles/
├── IVehicleService.cs        # Use-case sözleşmesi (Api bunu çağırır)
├── VehicleService.cs         # Orchestration: repo + UoW + mapper + audit.
│                             #   İş kuralı Domain'de, teknik iş port'larda.
├── Dtos/
│   ├── VehicleDto.cs         # Çıkış (response) modeli — entity ASLA dönülmez
│   ├── CreateVehicleRequest.cs
│   └── UpdateVehicleRequest.cs
├── Validators/
│   ├── CreateVehicleRequestValidator.cs   # FluentValidation
│   └── UpdateVehicleRequestValidator.cs
└── Mapping/
    └── VehicleMappingConfig.cs            # Mapster IRegister
```

## Kurallar

- `I<Feature>Service` **sözleşme**, `<Feature>Service` **implementasyon**'dur.
  Controller yalnızca `I<Feature>Service`'i tanır.
- Servis metotları `Task<Result<TDto>>` veya `Task<Result>` döner; beklenen
  hatalar exception yerine `Result.Failure(Error...)` ile taşınır.
- Birden çok repository'i değiştiren işlem `IUnitOfWork.BeginTransactionAsync()`
  ile sarmalanır.
- Giriş doğrulaması `Validators/` içindeki FluentValidation sınıflarındadır.
- Entity ⇄ DTO dönüşümü Mapster ile `Mapping/` altında tanımlanır.
- Servisler, kuruldukça `ECS.Application/DependencyInjection.cs` içinde DI'a eklenir.

> Bu klasörler PROMPT 1'de oluşturuldu; her feature'ın içeriği kendi prompt'unda
> bu desene göre doldurulacaktır.
