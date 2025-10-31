
# dotnet-ai-integration-lab

.NET projects demonstrating integration with **OpenAI APIs** and **Redis Stack** for vector search and AI-based document retrieval.

---

## 🧩 Project Overview

### DotNetOpenAIHello
Simple .NET console app demonstrating a direct call to OpenAI's Chat Completion API.

### RedisDemo
Console app that:
- Connects to Redis Stack (`localhost:6379`)
- Creates a RediSearch vector index (`idx:documents`)
- Generates embeddings using OpenAI’s API
- Stores and searches embeddings in Redis using `FT.SEARCH`

---

## ⚙️ Prerequisites

- **Operating System:** Windows 11 (tested)  
  macOS/Linux: should work with equivalent commands (not tested)
- **Required Software:**
  - [.NET SDK 9.0+](https://dotnet.microsoft.com/download)
  - [Docker Desktop](https://www.docker.com/products/docker-desktop)
  - A valid **OpenAI API key**

---

## 🔑 Environment Variable Setup

### Windows PowerShell

    powershell
    $env:OPENAI_API_KEY = "sk-your-api-key"
    
    
    macOS/Linux (untested)
    export OPENAI_API_KEY="sk-your-api-key"

Start Redis Stack (via Docker)

   

    Windows PowerShell
    docker run -d --name redis-stack -p 6379:6379 redis/redis-stack:latest
        
    macOS/Linux (untested)
    docker run -d --name redis-stack -p 6379:6379 redis/redis-stack:latest

🔍 Verify Redis is Running
Check container status

    docker ps --filter name=redis-stack

Check Redis connection

    docker exec -it redis-stack redis-cli PING

# Expected output: PONG

Check existing indexes

    docker exec -it redis-stack redis-cli FT._LIST

View container logs

    docker logs redis-stack --tail 100

Stop Redis container

    docker stop redis-stack

Remove Redis container

    docker rm redis-stack

🚀 Run the Applications
Run Redis Demo

    cd RedisDemo
    dotnet run


What it does:

Connects to Redis on localhost:6379

Ensures index idx:documents exists

Embeds sample documents

Prompts for a query and returns semantic search results

Run OpenAI Hello Demo

    cd DotNetOpenAIHello
    dotnet run


What it does:

Sends a simple prompt to the OpenAI API

Prints the model’s response in the console

🧰 Useful Redis Commands

    List indexes
    docker exec -it redis-stack redis-cli FT._LIST
    
    Show index info
    docker exec -it redis-stack redis-cli FT.INFO idx:documents
    
    Drop index and delete documents
    docker exec -it redis-stack redis-cli FT.DROPINDEX idx:documents DD
    
    Ping Redis
    docker exec -it redis-stack redis-cli PING
    
    Monitor Redis activity (debugging)
    docker exec -it redis-stack redis-cli MONITOR

🗂️ Repository Structure

    dotnet-ai-integration-lab/
    ├── DotNetOpenAIHello/
    │   ├── DotNetOpenAIHello.csproj
    │   └── Program.cs
    ├── RedisDemo/
    │   ├── RedisDemo.csproj
    │   └── Program.cs
    └── README.md

🧹 Cleanup Commands

    Stop Redis
    docker stop redis-stack
    
    Remove Redis
    docker rm redis-stack
    
    Remove all stopped containers (optional)
    docker container prune

🧾 Notes

All commands are verified on Windows 11 + PowerShell.

macOS/Linux equivalents are provided but not tested.

The Redis container runs on localhost:6379.

The .NET apps assume Redis is available locally; update the connection string in code if running elsewhere.

The OpenAI API key must be available in the environment before starting the applications.

License
This project is for educational and prototyping use.

---
 