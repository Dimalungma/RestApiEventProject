# REST API Event Project

## Назначение проекта

Учебный проект на **ASP.NET Core Web API**, реализующий базовый REST-сервис для управления мероприятиями (Events).

Сервис предоставляет стандартные операции:

-   создание события
-   получение списка событий
-   получение события по id
-   обновление события
-   удаление события
-   бронирование события

Сервис также поддерживает регистрацию пользователей, JWT-аутентификацию и разграничение доступа по ролям.

Данные приложения хранятся в **PostgreSQL**. Схема базы данных управляется через **миграции EF Core**.
------------------------------------------------------------------------

## Архитектура проекта

Проект разделён на четыре слоя по принципам Clean Architecture:

```text
RestApiEventProject.Domain
RestApiEventProject.Application
RestApiEventProject.Infrastructure
RestApiEventProject.Presentation
```

## Ролевая модель и разграничение прав

В системе используются две роли:

- `User` — обычный пользователь;
- `Admin` — администратор.

### Права пользователя

Пользователь с ролью `User` может:

- просматривать мероприятия;
- создавать бронирования;
- просматривать бронирования;
- отменять только собственные бронирования.

### Права администратора

Пользователь с ролью `Admin` может:

- выполнять все доступные обычному пользователю операции;
- создавать мероприятия;
- изменять мероприятия;
- удалять мероприятия;
- отменять бронирования любых пользователей.

### Ограничения бронирования

При создании бронирования действуют следующие правила:

- нельзя забронировать мероприятие, которое уже началось;
- у одного пользователя может быть не более 10 активных бронирований;
- активными считаются бронирования со статусами `Pending` и `Confirmed`;
- бронирования со статусами `Rejected` и `Cancelled` не учитываются в лимите.

---

### Domain

`RestApiEventProject.Domain` содержит предметную область приложения и не зависит от других проектов.

В слой входят:

- доменные сущности `Event` и `Booking`;
- перечисление `BookingStatus`;
- доменные результаты операций, например `ChangeTotalSeatsResult`;
- бизнес-правила, которые относятся к самим сущностям: резервирование мест, освобождение мест, изменение общего количества мест события, подтверждение и отклонение брони.

Domain не содержит зависимостей от ASP.NET Core, EF Core, PostgreSQL, DI-контейнера или других инфраструктурных технологий.

### Application

`RestApiEventProject.Application` содержит сценарии приложения и абстракции, необходимые для их выполнения. Слой зависит только от `Domain`.

В слой входят:

- сервисы/use cases: `EventService`, `BookingService`, `BookingProcessingService`;
- интерфейсы сервисов;
- интерфейсы портов для доступа к данным: `IEventRepository`, `IBookingRepository`;
- DTO, query/result/common-типы;
- мапперы;
- extension-метод `AddApplication()` для регистрации application-зависимостей в DI.

Application не зависит от Infrastructure и не содержит реализаций EF Core или других внешних технологий.

### Infrastructure

`RestApiEventProject.Infrastructure` содержит реализации портов и код, зависящий от внешних технологий.

В слой входят:

- `AppDbContext`;
- EF Core configuration-классы;
- реализации репозиториев `EventRepository` и `BookingRepository`;
- миграции EF Core;
- extension-метод `AddInfrastructure(...)` для регистрации DbContext и инфраструктурных реализаций;
- helper для применения миграций при запуске приложения.

Infrastructure зависит от `Application` и `Domain`.

### Presentation

`RestApiEventProject.Presentation` содержит HTTP-обвязку приложения и composition root.

В слой входят:

- контроллеры;
- middleware глобальной обработки исключений;
- `Program.cs`;
- настройки приложения;
- hosted service-обвязка для фоновой обработки бронирований.

Контроллеры принимают HTTP-запросы, вызывают сервисы из Application и маппят результаты use case в HTTP-ответы. Бизнес-логика в контроллерах не размещается.

`Program.cs` регистрирует зависимости через extension-методы `AddApplication()` и `AddInfrastructure(...)`.

## Зависимости между проектами

Зависимости направлены внутрь:

```text
Domain
↑
Application
↑
Infrastructure

Presentation → Application
Presentation → Infrastructure
```

Правила зависимостей:

- `Domain` ни от чего не зависит;
- `Application` зависит только от `Domain`;
- `Infrastructure` зависит от `Application` и `Domain`;
- `Presentation` зависит от `Application` и `Infrastructure`;
- `Application` не должен ссылаться на `Infrastructure`.

------------------------------------------------------------------------

## Используемые технологии

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Swagger / Swashbuckle.AspNetCore
- xUnit
- EF Core InMemory provider для unit-тестов
- Testcontainers + Docker для интеграционных тестов

------------------------------------------------------------------------

## Зависимости

Основные NuGet‑пакеты:

- `Microsoft.AspNetCore.App`
- `Swashbuckle.AspNetCore`
- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.EntityFrameworkCore.InMemory`
- `Testcontainers.PostgreSql`

Swagger используется для автоматической генерации документации и
тестирования API. **Подключен и работает только в Development**

------------------------------------------------------------------------

## Настройка подключения к БД
Для основного приложения нужен установленный и запущенный **PostgreSQL**.

По умолчанию приложение использует строку подключения из `appsettings.json`, например:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=postgres"
  }
}
```

Не забудьте изменить хост\порт и user\пароль на актуальные

## Настройка JWT

Параметры JWT задаются в `RestApiEventProject/appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "UseUserSecretsNotMe",
    "Issuer": "RestApiEventProject",
    "Audience": "RestApiEventProject",
    "LifetimeMinutes": 60
  }
}
```

Параметры:

- `Secret` — секретный ключ для подписи JWT;
- `Issuer` — издатель токена;
- `Audience` — аудитория токена;
- `LifetimeMinutes` — срок действия токена в минутах.

Для алгоритма `HS256` секрет должен содержать не менее 32 байт.

Значение из `appsettings.json` предназначено только для локального учебного запуска. Для разработки рекомендуется переопределить секрет через .NET User Secrets:

```bash
dotnet user-secrets init --project RestApiEventProject
dotnet user-secrets set "Jwt:Secret" "RestApiEventProject_Sprint8_Development_Secret_2026" --project RestApiEventProject
```

Проверить сохранённые значения:

```bash
dotnet user-secrets list --project RestApiEventProject
```

В production нельзя хранить рабочий JWT-секрет в публичном репозитории. Следует использовать безопасное случайное значение и передавать его через переменные окружения, secret storage платформы или другой защищённый механизм конфигурации.

Пример переменной окружения:

```text
Jwt__Secret=your-production-secret
```

---
------------------------------------------------------------------------
## Сборка проекта

``` bash
dotnet build
```

------------------------------------------------------------------------


## Запуск приложения
``` bash
dotnet run
```

При запуске приложение применяет миграции EF Core к базе данных через `Database.Migrate()`.

После запуска в консоли появятся адреса, на которых доступно приложение:

    Now listening on: https://localhost:7076
    Now listening on: http://localhost:5231

## Миграции EF Core

После переноса `AppDbContext` и миграций в `RestApiEventProject.Infrastructure` команды EF Core нужно выполнять с указанием проекта миграций и startup-проекта.

Создать новую миграцию:

```bash
dotnet ef migrations add MigrationName --project RestApiEventProject.Infrastructure --startup-project RestApiEventProject.Presentation
```

Применить миграции вручную:

```bash
dotnet ef database update --project RestApiEventProject.Infrastructure --startup-project RestApiEventProject.Presentation
```

Где:

- `--project RestApiEventProject.Infrastructure` — проект, в котором находится `AppDbContext` и хранятся миграции;
- `--startup-project RestApiEventProject.Presentation` — запускаемый проект, из которого берутся конфигурация, строка подключения и DI-настройки.

При обычном запуске приложения миграции применяются автоматически через infrastructure helper, вызываемый из `Program.cs`.


---

## Запуск unit-тестов приложения

```bash
dotnet test RestApiEventProject.Tests
```

Unit-тесты используют **EF Core InMemory provider**. Для их запуска PostgreSQL и Docker не нужны.

---

## Запуск интеграционных тестов

```bash
dotnet test RestApiEventProject.IntegrationTests
```

Интеграционные тесты используют **Testcontainers** и поднимают временный контейнер PostgreSQL.

Для запуска интеграционных тестов нужен установленный и запущенный **Docker** образ базы PostgreSQL, параметры подключения можно указать в PostgreSqlTestFixture.cs

------------------------------------------------------------------------

## Swagger UI

Swagger доступен в браузере по адресу:

```text
https://localhost:7076/swagger
```

Через Swagger можно:

- просматривать доступные эндпоинты;
- регистрировать пользователей;
- получать JWT-токен;
- авторизовывать последующие запросы;
- отправлять тестовые HTTP-запросы;
- смотреть структуру запросов и ответов API.

### Получение JWT-токена через Swagger

1. Выполнить `POST /auth/register`.

Пример регистрации обычного пользователя:

```json
{
  "login": "test-user",
  "password": "safePassword",
  "role": "User"
}
```

Поле `role` необязательно. Если оно не передано, используется роль `User`.

Для тестирования администратора можно указать:

```json
{
  "login": "test-admin",
  "password": "safePassword",
  "role": "Admin"
}
```

2. Выполнить `POST /auth/login`.

```json
{
  "login": "test-user",
  "password": "safePassword"
}
```

3. Скопировать значение `token` из ответа:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

4. Нажать кнопку `Authorize` в верхней части Swagger UI.

5. Вставить JWT-токен без префикса `Bearer`.

После авторизации Swagger автоматически добавляет к защищённым запросам заголовок:

```http
Authorization: Bearer <JWT-token>
```

Claims отдельно передавать не требуется. Идентификатор, логин и роль пользователя содержатся внутри JWT.

---

------------------------------------------------------------------------

# API


## Функционал создания мероприятий

### GET /events

Получение списка событий с фильтрацией и пагинацией.

#### Query параметры:
- `title` (string, опционально) — поиск по названию (частичное совпадение, без учёта регистра)
- `from` (DateTime, опционально) — события с датой начала >= указанной
- `to` (DateTime, опционально) — события с датой окончания <= указанной
- `page` (int, по умолчанию 1) — номер страницы
- `pageSize` (int, по умолчанию 10) — количество элементов на странице

#### Ответ:
```json
{
  "totalCount": 10,
  "page": 1,
  "currentItemCount": 5,
  "items": [ ... ]
}```

items содержит данные вида:
```json
{
  "id": 1,
  "title": "Встреча",
  "description": "Тусуемся с ребятами",
  "startAt": "2026-05-01T18:00:00",
  "endAt": "2026-05-01T20:00:00",
  "totalSeats": 10,
  "availableSeats": 7
}
```

### GET /events/{id}

Получение события по id.

Ответ:
- 200 OK — событие найдено
- 404 Not Found — событие не найдено

Для следующих эндпоинтов требуется роль `Admin`:

- `POST /events`;
- `PUT /events/{id}`;
- `DELETE /events/{id}`.

Возможные дополнительные ответы:

| Код | Описание |
|---|---|
| `401 Unauthorized` | JWT-токен отсутствует или недействителен |
| `403 Forbidden` | Пользователь аутентифицирован, но не имеет роли `Admin` |

`GET /events` и `GET /events/{id}` остаются публичными.

### POST /events
Тело запроса:
```json
{
  "title": "string",
  "description": "string",
  "startAt": "2026-04-10T10:00:00",
  "endAt": "2026-04-10T12:00:00",
  "totalSeats": 2
}
```

При создании события `availableSeats` автоматически устанавливается равным `totalSeats`.
`totalSeats` обязателен и должен быть больше 0.

### DELETE /events/{id}

Удаление события.

Ответ:

- `200 OK` - событие удалено;
- `404 Not Found` - событие не найдено.

---


### PUT /events/{id}

Обновление события.

При изменении `totalSeats` приложение сохраняет количество уже занятых мест:

- если `totalSeats` увеличен, `availableSeats` увеличивается на ту же разницу;
- если `totalSeats` уменьшен, `availableSeats` уменьшается с учётом уже занятых мест;
- если новое значение `totalSeats` меньше количества уже занятых мест, обновление отклоняется.

Ответ:

- `204 No Content` - событие обновлено;
- `404 Not Found` - событие не найдено;
- `409 Conflict` - новое значение `totalSeats` меньше количества уже занятых мест;
- `400 Bad Request` - некорректные данные.

---

## Функционал бронирования

Все эндпоинты бронирований требуют JWT-аутентификации.

### POST /events/{id}/book

Создаёт бронирование для текущего пользователя. Идентификатор пользователя берётся из claims JWT.

Возможные ответы:

| Код | Описание |
|---|---|
| `202 Accepted` | Бронирование создано |
| `400 Bad Request` | Мероприятие уже началось |
| `401 Unauthorized` | JWT-токен отсутствует или недействителен |
| `404 Not Found` | Мероприятие не найдено |
| `409 Conflict` | Нет свободных мест или достигнут лимит в 10 активных бронирований |

### GET /bookings/{id}

Возвращает бронирование по идентификатору.

Возможные ответы:

| Код | Описание |
|---|---|
| `200 OK` | Бронирование найдено |
| `401 Unauthorized` | JWT-токен отсутствует или недействителен |
| `404 Not Found` | Бронирование не найдено |

### DELETE /bookings/{id}

Отменяет бронирование.

Обычный пользователь может отменить только собственное бронирование. Администратор может отменить любое бронирование.

Возможные ответы:

| Код | Описание |
|---|---|
| `204 No Content` | Бронирование отменено |
| `401 Unauthorized` | JWT-токен отсутствует или недействителен |
| `403 Forbidden` | Пользователь пытается отменить чужое бронирование |
| `404 Not Found` | Бронирование не найдено |

### BookingStatus

- `Pending` — бронирование создано и ожидает обработки;
- `Confirmed` — бронирование подтверждено;
- `Rejected` — бронирование отклонено;
- `Cancelled` — бронирование отменено.
---


## Background

Фоновый сервис:

- периодически проверяет брони со статусом `Pending`;
- обрабатывает несколько броней параллельно;
- выполняет задержку для имитации обращения к внешнему сервису;
- переводит бронь в `Confirmed` при успешной обработке;
- переводит бронь в `Rejected` при ошибке обработки;
- при отклонении брони освобождает ранее зарезервированное место;
- заполняет `ProcessedAt`.

---

## Пример использования

1. Создать событие:
POST /events

2. Создать бронь:
POST /events/1/book

→ получить 202 + Location

3. Сразу проверить:
GET /bookings/1
→ статус Pending

4. Подождать несколько секунд

5. Проверить снова:
GET /bookings/1
→ статус Confirmed

## Ошибки

Необработанные исключения возвращаются в формате `ProblemDetails`.

Пример ответа:

```json
{
  "status": 500,
  "title": "Internal Server Error",
  "detail": "Описание ошибки",
  "instance": "/events/1/book"
}
```

Ошибки бизнес-логики возвращаются соответствующими HTTP-статусами:

- `400 Bad Request` - некорректные входные данные;
- `404 Not Found` - сущность не найдена;
- `409 Conflict` - конфликт текущего состояния, например отсутствие свободных мест или невозможность уменьшить `totalSeats`.

---

## Пример сценария с овербукингом

Допустим, существует мероприятие:

```json
{
  "id": 1,
  "title": "Tech Meetup",
  "totalSeats": 3,
  "availableSeats": 3
}
```

Последовательность запросов:

1. `POST /events/1/book`
2. `POST /events/1/book`
3. `POST /events/1/book`

Все три запроса успешно создадут бронирования.

Четвёртый запрос:

```http
POST /events/1/book
```

вернёт:

```http
409 Conflict
```

так как свободные места закончились.
