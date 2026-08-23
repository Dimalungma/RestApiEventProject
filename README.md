# REST API Event Project

## Назначение проекта

Учебный проект на ASP.NET Core Web API для управления мероприятиями, пользователями и бронированиями.

Начиная со sprint-9 приложение разделено на три независимых сервиса:

- `UsersService` — регистрация пользователей, вход и выдача JWT-токенов;
- `EventsService` — управление мероприятиями и количеством доступных мест;
- `BookingsService` — создание, обработка и отмена бронирований.

Каждый сервис владеет собственной PostgreSQL-базой данных. Прямых синхронных HTTP-вызовов между сервисами нет. Обмен между сервисами бронирований и мероприятий выполняется асинхронно через Kafka.

---

## Состав системы

| Сервис | Назначение | HTTP | HTTPS / Swagger | PostgreSQL |
| --- | --- | --- | --- | --- |
| `UsersService` | Регистрация, login, выдача JWT | `http://localhost:5101` | `https://localhost:7201/swagger` | `eventapi_users`, host port `5433` |
| `EventsService` | CRUD мероприятий, проверка и резервирование мест | `http://localhost:5102` | `https://localhost:7202/swagger` | `eventapi_events`, host port `5434` |
| `BookingsService` | Создание, обработка и отмена бронирований | `http://localhost:5103` | `https://localhost:7203/swagger` | `eventapi_bookings`, host port `5435` |

Инфраструктура запускается через `docker-compose.yml`:

- PostgreSQL 16 — отдельный контейнер для каждого сервиса;
- Apache Kafka — внешний адрес `localhost:9092`;
- ZooKeeper — используется Kafka в локальной инфраструктуре;
- Kafka и PostgreSQL имеют healthcheck-и;
- топики Kafka создаются приложением при запуске `EventsService`.

### Базы данных

Сервисы не используют общую БД:

```text
UsersService    → eventapi_users     → localhost:5433
EventsService   → eventapi_events    → localhost:5434
BookingsService → eventapi_bookings  → localhost:5435
```

Внутри контейнеров PostgreSQL работает на стандартном порту `5432`; разные host ports нужны для одновременного локального запуска трёх БД.

---

## Архитектура проекта

Каждый из трёх сервисов разделён на четыре слоя по принципам Clean Architecture:

```text
<Service>.Domain
<Service>.Application
<Service>.Infrastructure
<Service>.Presentation
```

Дополнительно используется общий проект:

```text
RestApiEventProject.Contracts
```

Он содержит только публичные контракты событий и общие имена Kafka-топиков.

### Domain

Содержит предметную область конкретного сервиса и не зависит от других проектов.

Примеры:

- `UsersService.Domain` — `User`, `UserRole`;
- `EventsService.Domain` — `Event`, локальная `BookingReservation`, результаты резервирования мест;
- `BookingsService.Domain` — `Booking`, `BookingStatus` и переходы между состояниями бронирования.

Domain не содержит зависимостей от ASP.NET Core, EF Core, Kafka, PostgreSQL или DI-контейнера.

### Application

Содержит сценарии приложения и абстракции, необходимые для их выполнения:

- сервисы/use cases;
- интерфейсы репозиториев;
- DTO, query/result/common-типы;
- мапперы;
- интерфейсы publisher/handler для межсервисного обмена.

Application зависит только от своего Domain и не знает о конкретной реализации Kafka или EF Core.

### Infrastructure

Содержит реализации портов и код, зависящий от внешних технологий:

- `AppDbContext` каждого сервиса;
- EF Core configuration-классы;
- реализации репозиториев;
- миграции EF Core;
- Kafka publishers и consumers;
- создание Kafka-топиков;
- реализацию JWT-генератора в `UsersService`;
- extension-метод `AddInfrastructure(...)`.

### Presentation

Содержит HTTP-обвязку и composition root:

- контроллеры;
- глобальную обработку исключений;
- `Program.cs`;
- `appsettings.json` и `launchSettings.json`;
- Swagger;
- JWT authentication/authorization;
- hosted-service обвязку, относящуюся к запуску приложения.

### Зависимости между проектами

Для каждого сервиса зависимости направлены внутрь:

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
- `Application` зависит только от своего `Domain`;
- `Infrastructure` зависит от своего `Application` и `Domain`;
- `Presentation` зависит от своего `Application` и `Infrastructure`;
- production-проекты одного сервиса не имеют `ProjectReference` на проекты другого сервиса;
- общий формат межсервисных сообщений находится в `RestApiEventProject.Contracts`.

---

## Kafka и межсервисный обмен

`BookingsService` и `EventsService` взаимодействуют по схеме choreography saga.

Используемые топики определены централизованно в `RestApiEventProject.Contracts`:

```text
booking-created
booking-cancelled
event-seat-reserved
event-seat-unavailable
booking-confirmed
booking-rejected
```

### Основной поток бронирования

```text
BookingsService
    Pending
      ↓
фоновая обработка / имитация оплаты
      ↓
AwaitingConfirmation
      ↓
BookingCreated
      ↓ Kafka
EventsService
      ↓
проверка мероприятия и свободных мест
      ↓
EventSeatReserved / EventSeatUnavailable
      ↓ Kafka
BookingsService
      ↓
Confirmed / Rejected
      ↓
BookingConfirmed / BookingRejected
```

Сервис бронирований не обращается к сервису мероприятий напрямую и сам не изменяет количество мест.

### `BookingConfirmed`

`BookingConfirmed` публикуется `BookingsService` после того, как:

1. ранее было опубликовано `BookingCreated`;
2. `EventsService` успешно проверил мероприятие и зарезервировал необходимое количество мест;
3. `EventsService` опубликовал `EventSeatReserved`;
4. `BookingsService` получил это сообщение и сохранил бронирование в статусе `Confirmed`;
5. после сохранения статуса `BookingsService` публикует `BookingConfirmed` в топик `booking-confirmed`.

Контракт содержит минимально необходимые публичные данные:

- идентификатор бронирования;
- идентификатор мероприятия;
- идентификатор пользователя;
- количество мест;
- момент подтверждения.

**В текущей choreography saga на `booking-confirmed` не подписан другой сервис.** Это финальное интеграционное событие, фиксирующее факт успешного подтверждения бронирования. `EventsService` не должен обрабатывать `BookingConfirmed`, потому что изменение количества свободных мест уже было выполнено раньше — при обработке `BookingCreated`, до отправки `EventSeatReserved`.

Таким образом, получение `BookingConfirmed` в текущей конфигурации не вызывает дополнительных изменений состояния системы. Контракт и топик оставлены как публичный факт, на который при необходимости может быть добавлен отдельный подписчик без изменения `BookingsService`.

### Отмена бронирования

Если отменяется бронь, для которой межсервисное подтверждение уже началось, `BookingsService` публикует `BookingCancelled`.

`EventsService` при получении сообщения:

- освобождает места, если они были зарезервированы;
- переводит локальную `BookingReservation` в состояние `Cancelled`;
- если `BookingCancelled` пришёл раньше `BookingCreated`, создаёт отменённую `BookingReservation` как tombstone, чтобы поздний `BookingCreated` не смог занять место.

`BookingReservation` также используется для идемпотентной обработки повторных Kafka-сообщений.

### Kafka producer и consumers

Kafka producer создаётся один раз на сервис и зарегистрирован как singleton. Он освобождается при остановке приложения.

Consumers реализованы как `BackgroundService`. Так как hosted service является singleton, scoped Application/repository/DbContext зависимости разрешаются через отдельный DI scope на обработку сообщения.

Offsets фиксируются после успешной обработки сообщения. Повторная доставка сообщения не должна повторно уменьшать количество мест благодаря локальной `BookingReservation` в `EventsService`.

---

## Статусы бронирования

`BookingStatus`:

- `Pending` — бронь создана и ожидает фоновой обработки;
- `AwaitingConfirmation` — локальная обработка прошла успешно, ожидается ответ `EventsService`;
- `Confirmed` — `EventsService` успешно зарезервировал место;
- `Rejected` — бронь отклонена;
- `Cancelled` — бронь отменена пользователем или администратором.

При подсчёте лимита активными считаются:

- `Pending`;
- `AwaitingConfirmation`;
- `Confirmed`.

У пользователя может быть не более 10 активных бронирований.

---

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

- выполнять доступные обычному пользователю операции;
- создавать мероприятия;
- изменять мероприятия;
- удалять мероприятия;
- отменять бронирования любых пользователей.

Для `POST` / `PUT` / `DELETE` эндпоинтов мероприятий используется роль `Admin`.

Все эндпоинты бронирований требуют JWT-аутентификации. `UserId` берётся из claims токена, а не передаётся клиентом отдельно.

---

## JWT

JWT выдаёт только `UsersService`.

`EventsService` и `BookingsService` токены не создают — они только проверяют их.

Во всех трёх сервисах должны совпадать:

```json
"Jwt": {
  "Secret": "UseUserSecretsNotMeAndIShouldBeVeryLongAndVarying",
  "Issuer": "RestApiEventProject",
  "Audience": "RestApiEventProject"
}
```

У `UsersService` дополнительно задаётся время жизни токена:

```json
"LifetimeMinutes": 60
```

Для локального учебного запуска значения находятся в `appsettings.json`. В production рабочий JWT-секрет не должен храниться в публичном репозитории; его следует передавать через переменные окружения или secret storage.

---

## Используемые технологии

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL 16
- Apache Kafka
- Confluent.Kafka
- Docker / Docker Compose
- JWT Bearer Authentication
- Swagger / Swashbuckle.AspNetCore

Swagger используется для автоматической генерации документации и тестирования API и доступен в `Development`.

---

# Запуск проекта

## 1. Требования

Для локального запуска нужны:

- .NET SDK 10;
- Docker Desktop;
- Docker Compose;
- Visual Studio с поддержкой ASP.NET Core либо любой другой способ запуска трёх Presentation-проектов.

Отдельно устанавливать PostgreSQL, Kafka или ZooKeeper на Windows не требуется.

---

## 2. Запуск инфраструктуры Docker

В корне репозитория находится:

```text
docker-compose.yml
```

Проверить конфигурацию:

```powershell
docker compose config
```

Запустить инфраструктуру:

```powershell
docker compose up -d
```

Проверить состояние контейнеров:

```powershell
docker compose ps
```

После запуска должны работать:

```text
eventapi-users-db      → localhost:5433
eventapi-events-db     → localhost:5434
eventapi-bookings-db   → localhost:5435
eventapi-kafka         → localhost:9092
eventapi-zookeeper
```

Следует дождаться состояния `healthy` у Kafka и трёх PostgreSQL-контейнеров перед запуском API.

Остановить инфраструктуру:

```powershell
docker compose down
```

Удалить контейнеры вместе с dev-данными PostgreSQL:

```powershell
docker compose down -v
```

`-v` удаляет volumes и данные локальных баз, поэтому эту команду следует использовать только когда данные не нужны.

---

## 3. Настройка подключений

Для запуска Presentation-проектов на host-машине используются следующие connection strings.

### `UsersService.Presentation/appsettings.json`

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5433;Database=eventapi_users;Username=postgres;Password=postgres"
}
```

### `EventsService.Presentation/appsettings.json`

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5434;Database=eventapi_events;Username=postgres;Password=postgres"
},
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "ConsumerGroup": "events-service"
}
```

### `BookingsService.Presentation/appsettings.json`

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5435;Database=eventapi_bookings;Username=postgres;Password=postgres"
},
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "ConsumerGroup": "bookings-service"
}
```

---

## 4. Сборка

Из корня решения:

```powershell
dotnet build
```

---

## 5. Запуск трёх API

Удобнее всего запускать три Presentation-проекта одновременно из одного экземпляра Visual Studio через **Multiple startup projects**:

```text
EventsService.Presentation
UsersService.Presentation
BookingsService.Presentation
```

Для каждого проекта нужно выбрать действие `Start` и профиль `https`.

`EventsService` желательно запускать первым, так как при старте он выполняет инициализацию Kafka-топиков.

Также каждый сервис можно запустить отдельно из терминала:

```powershell
dotnet run --project .\UsersService.Presentation\UsersService.Presentation.csproj --launch-profile https
```

```powershell
dotnet run --project .\EventsService.Presentation\EventsService.Presentation.csproj --launch-profile https
```

```powershell
dotnet run --project .\BookingsService.Presentation\BookingsService.Presentation.csproj --launch-profile https
```

При запуске сервисы автоматически применяют собственные EF Core migrations к своим базам данных.

---

## 6. Swagger UI

После запуска доступны три независимые Swagger-страницы:

```text
UsersService:
https://localhost:7201/swagger

EventsService:
https://localhost:7202/swagger

BookingsService:
https://localhost:7203/swagger
```

Через `UsersService` можно зарегистрировать пользователя и получить JWT-токен. Полученный токен принимается `EventsService` и `BookingsService`, поскольку все три сервиса используют одинаковые `Secret`, `Issuer` и `Audience`.

Если локальный HTTPS-сертификат ещё не настроен:

```powershell
dotnet dev-certs https --trust
```

---

## Получение JWT-токена через Swagger

1. Открыть Swagger `UsersService`:

```text
https://localhost:7201/swagger
```

2. Выполнить `POST /auth/register`.

Пример регистрации обычного пользователя:

```json
{
  "login": "test-user",
  "password": "safePassword",
  "role": "User"
}
```

Для администратора:

```json
{
  "login": "test-admin",
  "password": "safePassword",
  "role": "Admin"
}
```

3. Выполнить `POST /auth/login`.

```json
{
  "login": "test-user",
  "password": "safePassword"
}
```

4. Скопировать `token` из ответа.

5. В Swagger `EventsService` или `BookingsService` нажать `Authorize` и вставить JWT-токен.

Claims отдельно передавать не требуется. Идентификатор пользователя, логин и роль находятся внутри JWT.

---

# Миграции EF Core

Каждый сервис имеет собственный `AppDbContext` и собственную историю миграций.

Создание миграции выполняется с указанием Infrastructure как проекта миграций и Presentation как startup-проекта.

### UsersService

```powershell
dotnet ef migrations add MigrationName --project .\UsersService.Infrastructure\UsersService.Infrastructure.csproj --startup-project .\UsersService.Presentation\UsersService.Presentation.csproj --output-dir DataAccess\Migrations
```

### EventsService

```powershell
dotnet ef migrations add MigrationName --project .\EventsService.Infrastructure\EventsService.Infrastructure.csproj --startup-project .\EventsService.Presentation\EventsService.Presentation.csproj --output-dir DataAccess\Migrations
```

### BookingsService

```powershell
dotnet ef migrations add MigrationName --project .\BookingsService.Infrastructure\BookingsService.Infrastructure.csproj --startup-project .\BookingsService.Presentation\BookingsService.Presentation.csproj --output-dir DataAccess\Migrations
```

При обычном запуске сервисов миграции применяются автоматически через infrastructure helper, вызываемый из `Program.cs`.

---

# API

## UsersService

### POST /auth/register

Регистрация пользователя.

### POST /auth/login

Проверка логина/пароля и выдача JWT-токена.

---

## EventsService

### GET /events

Получение списка мероприятий с фильтрацией и пагинацией.

### GET /events/{id}

Получение мероприятия по идентификатору.

`GET`-эндпоинты мероприятий остаются публичными.

### POST /events

Создание мероприятия. Требуется роль `Admin`.

При создании `availableSeats` устанавливается равным `totalSeats`.

### PUT /events/{id}

Изменение мероприятия. Требуется роль `Admin`.

При изменении `totalSeats` сохраняется количество уже занятых мест. Нельзя установить `totalSeats` меньше количества уже занятых мест.

### DELETE /events/{id}

Удаление мероприятия. Требуется роль `Admin`.

---

## BookingsService

Все эндпоинты бронирований требуют JWT-аутентификации.

### POST /events/{id}/book

Создаёт бронирование текущего пользователя и возвращает `202 Accepted`.

Идентификатор пользователя берётся из claims JWT.

На стороне `BookingsService` проверяется лимит активных бронирований пользователя. Проверка существования мероприятия, времени начала и свободных мест выполняется асинхронно в `EventsService` после публикации `BookingCreated`, поэтому итоговый статус брони может измениться позже.

### GET /bookings/{id}

Возвращает бронирование и его текущий статус.

### DELETE /bookings/{id}

Отменяет бронирование.

Обычный пользователь может отменить только собственное бронирование. Администратор может отменить бронирование любого пользователя.

---

## Background processing

`BookingsService` содержит фоновую обработку бронирований:

- периодически получает бронирования `Pending`;
- обрабатывает несколько бронирований параллельно, каждое в отдельном DI scope;
- выполняет задержку для имитации обращения к внешнему сервису оплаты;
- при локальном отказе переводит бронь в `Rejected`;
- при успешной обработке переводит бронь в `AwaitingConfirmation`;
- после сохранения состояния публикует `BookingCreated`;
- повторяет публикацию для `AwaitingConfirmation`, если предыдущая отправка не была зафиксирована в `ConfirmationRequestedAt`.

Окончательный `Confirmed` выставляется только после положительного ответа `EventsService`.

---

## Ошибки

Необработанные технические исключения возвращаются в формате `ProblemDetails`.

Ошибки ожидаемой бизнес-логики не используются как механизм управления HTTP-потоком через исключения: Application возвращает result/error-типы, а Presentation преобразует их в соответствующие HTTP-ответы.

Типичные HTTP-статусы:

- `400 Bad Request` — некорректные входные данные;
- `401 Unauthorized` — JWT отсутствует или недействителен;
- `403 Forbidden` — недостаточно прав;
- `404 Not Found` — сущность не найдена;
- `409 Conflict` — конфликт текущего состояния.

---