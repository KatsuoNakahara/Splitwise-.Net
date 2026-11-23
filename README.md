# Splitwise API

Це навчальний проєкт: копія сервісу Splitwise. Це Web API на .NET та Entity Framework Core.
Дозволяє створити групу друзів, додавати спільні витрати, автоматично розраховувати баланси та погашати борги.

## Що вміє

- Реєстрація та вхід (ASP.NET Core Identity + JWT Bearer токен)
- Створення груп та додавання учасників за email
- Додавання витрат:
  - **Equal** — порівну між усіма або обраними учасниками (з точним розподілом до копійки)
  - **Exact** — з точним зазначенням суми для кожного учасника
- Перегляд балансів та спрощений розрахунок взаємозаліків ("хто кому скільки винен")
- Погашення боргів (Settlements)

## Архітектура (Трирівнева архітектура)

Проєкт побудований за принципами трирівневої / чистої архітектури:

- `Splitwise.Domain` — доменні сутності (`Group`, `GroupMember`, `Expense`, `ExpenseSplit`, `Settlement`). Не залежить від інших проєктів.
- `Splitwise.Application` — бізнес-логіка, інтерфейси сховищ, валідатори, алгоритми розрахунку боргів (`BalanceCalculator`, `SplitCalculator`), сервіси та DTO.
- `Splitwise.Infrastructure` — реалізація роботи з базою даних (`AppDbContext` на базі EF Core та SQLite), користувачі (`ApplicationUser`), реалізація автентифікації (`AuthService`, `UserDirectory`).
- `Splitwise.Api` — представлення Web API (контролери `AuthController`, `GroupsController`, `ExpensesController`, `BalancesController`), middleware обробки помилок (`ExceptionHandlingMiddleware`), конфігурація JWT та OpenAPI.
- `Splitwise.Tests` — модульні та інтеграційні тести (NUnit).

## Як запустити

```bash
# Збірка проєкту
dotnet build

# Запуск тестів
dotnet test

# Запуск API
dotnet run --project src/Splitwise.Api
```

API за замовчуванням стартує на `http://localhost:5080`. База даних SQLite (`splitwise.db`) створюється автоматично при старті.
Приклади HTTP-запитів знаходяться у файлі `src/Splitwise.Api/Splitwise.Api.http`.

## Ендпоїнти

| Метод | URL | Опис |
|---|---|---|
| POST | `/api/auth/register` | Реєстрація нового користувача |
| POST | `/api/auth/login` | Вхід та отримання JWT токена |
| POST | `/api/groups` | Створити нову групу |
| GET | `/api/groups` | Отримати список груп користувача |
| GET | `/api/groups/{id}` | Деталі групи та список учасників |
| POST | `/api/groups/{id}/members` | Додати учасника до групи по email |
| POST | `/api/groups/{id}/expenses` | Додати витрату (Equal або Exact) |
| GET | `/api/groups/{id}/expenses` | Список витрат групи |
| GET | `/api/groups/{id}/balances` | Баланси учасників та спрощений граф боргів |
| POST | `/api/groups/{id}/settlements` | Погашення боргу |

## Валідація та бізнес-правила

- Назва групи обов'язкова і не перевищує 100 символів; email учасників мають бути унікальними; максимум 50 учасників у групі.
- Сума витрати строго більша за 0, з точністю максимум 2 знаки після коми; платник обов'язково є учасником групи.
- При точній розбивці (`Exact`) сума часток повинна суворо збігатися із загальною сумою витрати.
- Не можна погасити борг самому собі або особі поза групою.
- Доступ до групи мають виключно її учасники (повертається 403 Forbidden).

## CI/CD

Налаштовано GitHub Actions workflow (`.github/workflows/ci.yml`), який на кожен push та pull request автоматично виконує `dotnet restore`, `dotnet build` та `dotnet test`.
