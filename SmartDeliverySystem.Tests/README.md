# SmartDeliverySystem Tests

Цей проект містить комплексні тести для Smart Delivery System.

## 📋 Структура тестів

### Integration Tests (Інтеграційні тести)
- **ApiIntegrationTests** - тестування API endpoints через HTTP запити

### Service Tests (Тести сервісів)
- **DeliveryServiceTests** - тести бізнес-логіки доставок
- **GpsTrackingTests** - тести GPS трекінгу та валідації координат

### Business Logic Tests (Тести бізнес-логіки)
- **BusinessRulesTests** - тести правил доставки, статусів, інвентаря

## 🛠️ Використані технології

- **xUnit** - тестовий фреймворк
- **FluentAssertions** - для читабельних assertions
- **Microsoft.EntityFrameworkCore.InMemory** - in-memory база даних для тестів
- **Microsoft.AspNetCore.Mvc.Testing** - для інтеграційних тестів

## 🚀 Запуск тестів

### Всі тести
```bash
dotnet test
```

### Тільки інтеграційні тести
```bash
dotnet test --filter "Integration"
```

### Тільки unit тести
```bash
dotnet test --filter "Services|BusinessLogic"
```

### З покриттям коду
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Що тестується

### API Integration Tests
- ✅ GET/POST операції для всіх endpoints
- ✅ Валідація HTTP статус кодів
- ✅ End-to-end workflow тестування
- ✅ Обробка помилок (404, 400)

### Service Tests
- ✅ Delivery Service операції
- ✅ GPS координати валідація
- ✅ Історія переміщень
- ✅ Розрахунок відстаней

### Business Logic Tests
- ✅ Розрахунок сум доставки
- ✅ Валідація статусів доставки
- ✅ Правила переходу статусів
- ✅ Управління інвентарем
- ✅ Бізнес-правила скасування

## 🎯 Приклади тестових сценаріїв

### End-to-end API тестування
1. Створення вендора
2. Створення магазину
3. Створення продукту
4. Валідація даних
5. Обробка помилок

### GPS трекінг тестування
1. Валідація координат
2. Розрахунок відстаней
3. Детекція прибуття
4. Історія переміщень

### Бізнес-логіка тестування
1. Розрахунок вартості
2. Переходи статусів
3. Оновлення інвентаря
4. Правила скасування

## 📈 Метрики тестів

- **Integration Tests**: ~15 HTTP endpoint тестів
- **Service Tests**: ~15 unit тестів сервісів
- **Business Logic Tests**: ~10 тестів бізнес-правил
- **Total**: ~40 тестів
- **Execution Time**: < 20 секунд

## 🔧 Особливості тестів

### Integration Tests
- Реальні HTTP запити до API
- Тестування з WebApplicationFactory
- Валідація response кодів та даних
- End-to-end workflow перевірка

### Service Tests
- In-memory база даних
- Ізольовані unit тести
- Тестування GPS логіки
- Валідація координат

### Business Logic Tests
- Тестування бізнес-правил
- Валідація статусних переходів
- Інвентарні операції
- Розрахунки сум

## 🎉 Результат

Тести забезпечують:
- ✅ Надійність API endpoints
- ✅ Коректність GPS трекінгу
- ✅ Правильність бізнес-логіки
- ✅ Валідацію даних
- ✅ Швидке виявлення помилок
