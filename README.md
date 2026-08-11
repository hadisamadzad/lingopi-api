# 🧹 Lingopi API

Lingopi is a modern headless blog API designed using Clean Architecture principles. It provides a structured backend for managing articles, tags, and subscriptions, making it easy to integrate with any frontend.

## 🚀 Overview

**Lingopi** is a **headless blog backend** built with **Clean Architecture** 🏗️. It provides a structured API for managing **articles**, **tags**, and **subscriptions**, making it easy to integrate with any frontend or mobile app.

💡 Whether you're building a **personal blog**, an **editorial platform**, or a **content-driven application**, Lingopi serves as a **powerful and extensible backend**. 🚀

## ✨ Features

- 🏗 **Headless Blog** – Decoupled backend for seamless frontend integration
- 📝 **Article Management** – Create, update, delete, and publish blog posts
- 🔖 **Tagging System** – Categorize articles with tags for better organization
- 🔍 **Full-Text Search** – Easily search articles using keywords
- 📩 **Email Subscriptions** – Let users subscribe for new article updates
- 🚀 **Clean Architecture** – Maintainable, scalable, and testable structure
- ⚡  **Logical CQRS with MediatR** – Separation of concerns for better performance
- 🗄 **MongoDB Storage** – Flexible NoSQL database for blog content
- 🌐 **RESTful API** – Well-structured API endpoints for frontend or third-party use
- 🔄 **Soft & Hard Deletion** – Manage content with reversible actions

## 🏗️ Architecture

This project follows the Clean Architecture pattern and the application is divided into the following layers. For the sake of simplicity, layers are divided by directories rather than Class Library projects.

### Application Layer

- **🧠 Types**: Contains entities, models and DTOs
- **⚙️ UseCases**: Contains application-specific business rules and use cases

### Infrastructure Layer

- **💾 Database**: Handles data storage and retrieval
- **🔌 API**: Manages external interfaces for the application

### Presentation Layer

- **🔗 API**: The API entry point

## 🛠️ Tech Stack

### 🔙 Backend

- ASP.NET Core Web API with **Minimal APIs**
- **.NET 9**
- API Gateway: **Ocelot**
- Database: **MongoDB** with MongoDB.Driver
- **Repository Manager**
- Cache: **Redis**
- Authentication: **Simple User/Role JWT Auth**
- Swagger
- Health Checks
- Serilog
- **ULID**
- Validation with FluentValidation
- AES Encryption
- **LockManager**
- **OperationResult** Pattern

### 🐳 DevOps & Tools

- **Docker** – Because we love shipping things (in containers)
- **GitHub Actions** – Automating all the things
- **NGINX** – Proxy things like a boss

## 🚀 Getting Started

### Local Docker debug setup

The local Compose file extends `docker-compose.yml` with container-safe Local settings for Redis and service ports. Run these commands from the repository root.

Build and start all services:

```bash
ENV=Local docker compose -f docker-compose.yml -f docker-compose.local.yml up --build -d
```

Start the existing images without rebuilding:

```bash
ENV=Local docker compose -f docker-compose.yml -f docker-compose.local.yml up -d
```

Check service status:

```bash
ENV=Local docker compose -f docker-compose.yml -f docker-compose.local.yml ps
```

Follow Gateway logs:

```bash
ENV=Local docker compose -f docker-compose.yml -f docker-compose.local.yml logs -f gateway
```

Check service health:

```bash
curl http://localhost:45000/api/health
curl http://localhost:45000/api/identity/health
curl http://localhost:45000/api/lingo/health
```

Open Swagger:

- Identity: `http://localhost:45000/api/identity/swagger/index.html`
- Lingo: `http://localhost:45000/api/lingo/swagger/index.html`

Stop the local stack:

```bash
ENV=Local docker compose -f docker-compose.yml -f docker-compose.local.yml down
```

### Installation

1. Clone the repository

   ```bash
   git clone https://github.com/hadisamadzad/lingopi-clean-architecture.git
   cd lingopi-clean-architecture
   ```

2. Install dependencies

   ```bash
   [Your installation commands]
   ```

3. Configure environment variables

   ```bash
   cp .env.example .env
   [Update .env with your settings]
   ```

4. Run database migrations

   ```bash
   [Your migration commands]
   ```

5. Start the application

   ```bash
   [Your startup commands]
   ```

## 📂 Project Structure

```
./
├── src/
│   ├── Common/
│   │   ├── BusContracts/
│   │   ├── Extensions/
│   │   ├── Helpers/
│   │   ├── Interfaces/
│   │   ├── Persistence/
│   │   └── Utilities/
│   ├── Gateway/
│   │   └── Core/
│   ├── Identity/
│   │   ├── Core/
│   │   ├── Application/
│   │   ├── Infrastructure/
│   │   └── Api/
│   └── Blog/
│       ├── Core/
│       ├── Application/
│       ├── Infrastructure/
│       └── Api/
└── test/
    ├── Common.Test/
    ├── Gateway.Test/
    ├── Identity.Test/
    └── Blog.Test/
```

## 🧪 Testing

```bash
dotnet test Lingopi.slnx
```

## 🤝 Contributing

We welcome contributions from the community! If you’d like to help improve **Lingopi**, here’s how you can contribute:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### 📝 Contribution Guidelines

- Follow the existing code style and architecture
- Ensure all tests pass before submitting
- Write clear commit messages

Got an idea? Found a bug? Open an issue and let’s talk!

Looking forward to your contributions! 🚀🔥

## 📜 License

This project is licensed under the MIT License.
