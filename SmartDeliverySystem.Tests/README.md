# SmartDeliverySystem.Tests

Цей проєкт містить комплексні тести для системи SmartDeliverySystem.

## Структура тестів

### Unit тести
- **Services/DeliveryServiceTests.cs** - Тести для основного сервісу доставки
- **Services/ServiceBusServiceTests.cs** - Тести для Azure Service Bus сервісу
- **Services/SignalRServiceTests.cs** - Тести для SignalR сервісу

### Controller тести
- **Controllers/DeliveryControllerTests.cs** - Тести для контролера доставки
- **Controllers/VendorsControllerTests.cs** - Тести для контролера продавців
- **Controllers/StoresControllerTests.cs** - Тести для контролера магазинів
- **Controllers/ProductsControllerTests.cs** - Тести для контролера товарів

### Integration тести
- **Integration/DeliveryIntegrationTests.cs** - End-to-end тести для повних HTTP запитів

## Допоміжні класи
- **BaseTest.cs** - Базовий клас для налаштування БД та AutoMapper
- **TestDataHelper.cs** - Фабрика для створення тестових даних
- **GlobalUsings.cs** - Глобальні using директиви

## Запуск тестів

```bash
# Запуск всіх тестів
dotnet test

# Запуск з деталями
dotnet test --logger "console;verbosity=detailed"

# Запуск конкретного класу тестів
dotnet test --filter "ClassName=DeliveryServiceTests"

# Запуск з покриттям коду
dotnet test --collect:"XPlat Code Coverage"
```

## Покриття тестами

### Основна функціональність:
✅ Створення доставки
✅ Оновлення статусу доставки
✅ Обробка платежів
✅ Пошук найкращого магазину
✅ GPS трекінг
✅ Управління продавцями
✅ Управління магазинами
✅ Управління товарами
✅ Azure Service Bus інтеграція
✅ SignalR real-time сповіщення

### Валідація та Error Handling:
✅ Неіснуючі ID
✅ Неправильні суми платежів
✅ Бізнес-логіка валідації
✅ Cascade видалення
✅ Дублікати записів
