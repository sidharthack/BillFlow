# 🚀 BillFlow

> Production-grade Multi-Tenant Invoice & Billing SaaS built with **.NET 10 Microservices**, **React**, **Azure**, and **Event-Driven Architecture**.

![.NET](https://img.shields.io/badge/.NET-10-purple)
![React](https://img.shields.io/badge/React-18-blue)
![Azure](https://img.shields.io/badge/Azure-Cloud-blue)
![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Event%20Driven-orange)
![License](https://img.shields.io/badge/License-MIT-green)

---

## 📖 Overview

BillFlow is a complete SaaS billing platform designed around modern cloud-native principles.

The platform enables organisations to manage customers, generate professional invoices, track payments, automate notifications, and monitor system health through a fully observable microservice ecosystem.

Built as an 8-week engineering project, BillFlow demonstrates:

* Multi-tenant SaaS architecture
* Microservice communication via events
* Secure JWT authentication
* API Gateway patterns
* Distributed observability
* Automated cloud deployment
* Production-ready infrastructure

---

## ✨ Key Features

### 🏢 Multi-Tenant SaaS

* Tenant isolation at the database layer
* Workspace-based organisation management
* Secure tenant-scoped data access
* Role-based authorization

### 🧾 Invoice Management

* Draft → Sent → Paid → Overdue → Cancelled workflow
* Automatic invoice numbering
* Tax calculations
* Professional PDF generation
* Invoice analytics dashboard

### 📧 Automated Notifications

* Event-driven email delivery
* RabbitMQ-based messaging
* Invoice created notifications
* Payment reminders
* Overdue invoice alerts

### ⏰ Background Processing

* Daily overdue invoice detection
* Scheduled Hangfire jobs
* Retry handling
* Job monitoring dashboard

### 📊 Observability

* Structured logging with Serilog
* Correlation ID propagation
* Centralized logs in Seq
* Prometheus metrics
* Grafana dashboards

### ☁️ Cloud Native Deployment

* Dockerized services
* Azure App Service hosting
* Azure SQL databases
* GitHub Actions CI/CD
* Azure Container Registry

---

# 🏗 Architecture

                          React Frontend
                                 │
                                 ▼
                    ┌─────────────────────┐
                    │  YARP API Gateway   │
                    │ Rate Limiting       │
                    │ Security Headers    │
                    │ Correlation IDs     │
                    └──────────┬──────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        ▼                      ▼                      ▼

 ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
 │ TenantSvc   │      │ IdentitySvc │      │ CustomerSvc │
 └─────────────┘      └─────────────┘      └─────────────┘

        ┌──────────────────────────────────────┐
        ▼                                      ▼

 ┌─────────────┐                     ┌─────────────────┐
 │ InvoiceSvc  │◄────RabbitMQ───────►│ NotificationSvc │
 └─────────────┘                     └─────────────────┘

        │
        ▼

 SQL Server Databases

        │
        ▼

 Seq • Prometheus • Grafana
```

---

# 🛠 Technology Stack

## Backend

| Technology             | Purpose                |
| ---------------------- | ---------------------- |
| ASP.NET Core (.NET 10) | Microservices          |
| Entity Framework Core  | Data access            |
| SQL Server             | Persistence            |
| YARP                   | API Gateway            |
| RabbitMQ               | Event messaging        |
| Hangfire               | Background jobs        |
| QuestPDF               | Invoice PDF generation |
| JWT Authentication     | Security               |
| Serilog + Seq          | Logging                |
| Prometheus             | Metrics                |
| Grafana                | Dashboards             |

---

## Frontend

| Technology      | Purpose       |
| --------------- | ------------- |
| React 18        | UI            |
| TypeScript      | Type safety   |
| Vite            | Build tooling |
| Tailwind CSS    | Styling       |
| TanStack Query  | Server state  |
| React Hook Form | Forms         |
| Zod             | Validation    |
| Recharts        | Analytics     |

---

## Infrastructure

| Technology               | Purpose             |
| ------------------------ | ------------------- |
| Docker                   | Containerization    |
| Docker Compose           | Local orchestration |
| Azure App Service        | Hosting             |
| Azure SQL                | Managed database    |
| Azure Container Registry | Image storage       |
| GitHub Actions           | CI/CD               |

---

# 🔧 Microservices

| Service             | Port | Responsibility                |
| ------------------- | ---- | ----------------------------- |
| Gateway             | 5100 | Routing, rate limiting        |
| TenantService       | 5001 | Tenant & workspace management |
| IdentityService     | 5002 | Authentication & JWT          |
| CustomerService     | 5003 | Customer management           |
| InvoiceService      | 5000 | Billing domain                |
| NotificationService | 5004 | Email notifications           |

---

# 🔐 Security

* JWT Access Tokens
* Refresh Token Flow
* Tenant-aware authorization
* API Gateway protection
* Security headers
* Rate limiting
* Correlation tracking

---

# 📦 Local Development

### Prerequisites

* .NET 10 SDK
* Node.js 20+
* Docker Desktop
* SQL Server

### Start Infrastructure

```bash
cd infra
docker compose -f docker-compose.infra.yml up -d
```

### Run Database Migrations

```bash
dotnet ef database update
```

Run once for each service.

### Start Backend Services

```bash
dotnet run
```

Launch each service in a separate terminal.

### Start Frontend

```bash
cd frontend

npm install
npm run dev
```

Frontend:

```text
http://localhost:3000
```

---

# 🐳 Run Entire Platform

```bash
docker compose up -d --build
```

Available endpoints:

| Service     | URL                    |
| ----------- | ---------------------- |
| Application | http://localhost:3000  |
| API Gateway | http://localhost:5100  |
| Seq         | http://localhost:5341  |
| Prometheus  | http://localhost:9090  |
| Grafana     | http://localhost:3001  |
| RabbitMQ    | http://localhost:15672 |

---


# 🚀 CI/CD Pipeline

Every push to `main` automatically:

1. Runs all tests
2. Builds Docker images
3. Pushes images to Azure Container Registry
4. Deploys microservices
5. Performs health validation

---

# ☁️ Deployment

BillFlow is deployed on Azure using:

* Azure App Service
* Azure SQL
* Azure Container Registry
* GitHub Actions

Production deployments are fully automated through CI/CD.

---

# 📈 Observability

The platform provides complete operational visibility.

### Logging

* Structured logs
* Correlation IDs
* Request tracing
* Centralized Seq dashboard

### Metrics

* Request throughput
* Error rates
* Service health
* Queue metrics

### Dashboards

* Grafana visualization
* Prometheus data collection

---

# 📂 Repository Structure

```text
BillFlow
│
├── BillFlow.Gateway
├── BillFlow.TenantService
├── BillFlow.IdentityService
├── BillFlow.CustomerService
├── BillFlow.InvoiceService
├── BillFlow.NotificationService
├── BillFlow.Contracts
├── frontend
├── infra
└── tests
```

---

# 🎯 Learning Outcomes

This project was built to gain hands-on experience with:

* Microservice Architecture
* Multi-Tenant SaaS Design
* Event-Driven Systems
* Cloud Deployment
* Observability
* Distributed Systems
* CI/CD Automation
* Production Engineering Practices

---


# 👨‍💻 Author

Built as a full-stack cloud engineering project focused on modern SaaS architecture and production-ready development practices.
