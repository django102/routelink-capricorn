# routelink-capricorn
RouteLink Capricorn Integration

A .NET Core microservice for integrating with Capricorn's API.

## Features

- Purchase airtime, data, and TV subscriptions
- Secure JWT authentication
- Idempotent transactions
- Comprehensive logging
- Redis caching
- MongoDB for audit logs

## Prerequisites

- Docker
- .NET 9 SDK
- SQL Server (via Docker)
- MongoDB (via Docker)
- Redis (via Docker)

## Installation

1. Clone the repository
2. Configure environment variables (see `.env.example`)
3. Run `docker-compose up -d`

## Switching Providers

To switch to a different provider:

1. Implement the new provider service (similar to `CapricornService`)
2. Update the configuration to point to the new provider
3. The system will automatically use the new provider based on configuration

## API Documentation

Access Swagger UI at `http://localhost:5000/swagger` after starting the services.


## Monitoring and Alerting

The system includes comprehensive monitoring:

1. Access Grafana at `http://localhost:3000` (default credentials: admin/admin)
2. Pre-configured dashboards for:
   - Service health
   - Transaction metrics
   - Error rates
   - Fraud detection alerts

## Fraud Detection

The fraud detection system evaluates transactions based on:

1. Transaction amount anomalies
2. Frequency of transactions
3. Recipient patterns
4. Time-of-day analysis
5. Spending velocity

To test fraud detection:

```bash
curl -X POST http://localhost:5003/api/fraud-detection/evaluate \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"userId":"user1","transactionType":"Airtime","amount":15000,"recipient":"1234567890"}'