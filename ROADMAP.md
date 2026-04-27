# 🗺️ Roadmap Proiect NutritionAdvisor

## 📊 Status Actual

### ✅ Ce există deja:
- **Clean Architecture** parțial implementată (Domain, Application, Infrastructure, API)
- **Blazor WebAssembly** frontend cu MudBlazor
- **Autentificare JWT** funcțională
- **Entități de bază**: User, UserProfile, Reteta
- **PostgreSQL** database cu Entity Framework Core
- **MediatR** pentru CQRS (parțial implementat)
- **CORS** configurat pentru comunicare Frontend-Backend

### ❌ Ce lipsește:
- Modul Python AI (complet)
- CQRS complet pentru toate entitățile
- Entități extinse (Menus, Allergies, FoodPreferences, MealPlans)
- Sistem de abonament Premium
- Docker & containerizare
- Tests (Unit & Integration)
- CI/CD Pipeline
- Cloud Deployment
- SonarQube integration

---

## 🎯 Faze de Implementare

## FAZA 1: Extinderea Arhitecturii Backend (.NET 10) ⏱️ 1-2 săptămâni

### 1.1 Entități Domain (Domain Layer)

**Prioritate: CRITICAL**

Fișiere noi în `NutritionAdvisor.Domain/Entities/`:

```
✅ User.cs (există)
✅ UserProfile.cs (există)
✅ Reteta.cs (există - necesită extindere)
🆕 Recipe.cs (refactorizare din Reteta)
🆕 FoodPreference.cs
🆕 Allergy.cs
🆕 Menu.cs
🆕 MealPlan.cs (planuri zilnice)
🆕 WeeklyMealPlan.cs (planuri săptămânale)
🆕 Ingredient.cs
🆕 RecipeIngredient.cs (relație many-to-many)
🆕 Subscription.cs (pentru premium)
🆕 Payment.cs
🆕 AIRecommendation.cs
```

**Detalii entități:**

**Recipe** (extindere din Reteta):
- Id, Title, Description, Calories, Protein, Carbs, Fats
- CookingTime, Difficulty, ImageURL
- IsVegetarian, IsVegan, IsGlutenFree
- CreatedBy (userId), CreatedAt, UpdatedAt
- IsPremium (bool)
- Category (enum: Breakfast, Lunch, Dinner, Snack)

**FoodPreference**:
- Id, UserId
- DietType (enum: Vegetarian, Vegan, Keto, Paleo, Mediterranean, etc.)
- PreferredCuisines (JSON/List: Italian, Asian, Mexican, etc.)
- DislikedIngredients (List<string>)

**Allergy**:
- Id, UserId
- AllergenName (ex: Peanuts, Dairy, Gluten, Shellfish)
- SeverityLevel (enum: Mild, Moderate, Severe)

**Menu**:
- Id, Name, Description
- Recipes (List<Recipe>)
- TotalCalories, TotalProtein, TotalCarbs, TotalFats
- CreatedBy, CreatedAt

**MealPlan** (plan zilnic):
- Id, UserId, Date
- Breakfast (Recipe), Lunch (Recipe), Dinner (Recipe)
- Snacks (List<Recipe>)
- TotalCalories, TargetCalories
- IsAIGenerated (bool)
- CreatedAt

**WeeklyMealPlan**:
- Id, UserId
- StartDate, EndDate
- DailyPlans (List<MealPlan>)
- GeneratedByAI (bool)

**Subscription**:
- Id, UserId
- SubscriptionType (enum: Free, Premium)
- StartDate, EndDate
- IsActive, AutoRenew
- PaymentMethod

**AIRecommendation**:
- Id, UserId
- RecommendationType (enum: Recipe, MealPlan, NutritionalAdvice)
- Content (JSON)
- GeneratedAt
- Feedback (string?)

### 1.2 Application Layer - CQRS Complet

**Prioritate: CRITICAL**

Structură în `NutritionAdvisor.Application/`:

```
Commands/
  Recipes/
    🆕 CreateRecipe/
      - CreateRecipeCommand.cs
      - CreateRecipeCommandHandler.cs
      - CreateRecipeCommandValidator.cs
    🆕 UpdateRecipe/
    🆕 DeleteRecipe/
  
  MealPlans/
    🆕 CreateDailyMealPlan/
      - CreateDailyMealPlanCommand.cs
      - CreateDailyMealPlanCommandHandler.cs
      - CreateDailyMealPlanCommandValidator.cs
    🆕 CreateWeeklyMealPlan/
    🆕 UpdateMealPlan/
    🆕 DeleteMealPlan/
  
  FoodPreferences/
    🆕 SetFoodPreferences/
    🆕 UpdateFoodPreferences/
  
  Allergies/
    🆕 AddAllergy/
    🆕 RemoveAllergy/
  
  Subscriptions/
    🆕 UpgradeToPremium/
    🆕 CancelSubscription/
    🆕 ProcessPayment/
  
  AI/
    🆕 RequestAIRecommendation/
    🆕 GeneratePersonalizedMealPlan/

Queries/
  Recipes/
    🆕 GetAllRecipes/
      - GetAllRecipesQuery.cs
      - GetAllRecipesQueryHandler.cs
    🆕 GetRecipeById/
    🆕 GetRecipesByFilter/
    🆕 GetPremiumRecipes/
  
  MealPlans/
    🆕 GetUserMealPlans/
    🆕 GetWeeklyMealPlan/
    🆕 GetMealPlanByDate/
  
  FoodPreferences/
    🆕 GetUserFoodPreferences/
  
  Allergies/
    🆕 GetUserAllergies/
  
  Subscriptions/
    🆕 GetUserSubscription/
  
  AI/
    🆕 GetAIRecommendations/

DTOs/
  🆕 RecipeDto.cs
  🆕 MealPlanDto.cs
  🆕 FoodPreferenceDto.cs
  🆕 AllergyDto.cs
  🆕 SubscriptionDto.cs
  🆕 AIRecommendationDto.cs

Validators/ (FluentValidation)
  🆕 CreateRecipeValidator.cs
  🆕 CreateMealPlanValidator.cs
  etc.
```

### 1.3 Infrastructure Layer

**Prioritate: CRITICAL**

În `NutritionAdvisor.Infrastructure/`:

```
Repositories/
  ✅ UserRepository.cs (există)
  ✅ UserProfileRepository.cs (există)
  🆕 RecipeRepository.cs
  🆕 MealPlanRepository.cs
  🆕 FoodPreferenceRepository.cs
  🆕 AllergyRepository.cs
  🆕 SubscriptionRepository.cs
  🆕 AIRecommendationRepository.cs

Databases/
  ✅ ApplicationDbContext.cs (există - extinde)
  🆕 Configurations/
    - RecipeConfiguration.cs
    - MealPlanConfiguration.cs
    - etc. (Entity Type Configurations)

Migrations/ (EF Core)
  - Add_Recipe_Tables.cs
  - Add_MealPlan_Tables.cs
  - etc.

ExternalServices/
  🆕 PythonAIService.cs (client pentru API-ul Python)
  🆕 PaymentService.cs (integrare cu Stripe sau altceva)
```

### 1.4 API Controllers

**Prioritate: CRITICAL**

În `NutritionAdvisor.API/Controllers/`:

```
✅ AuthController.cs (există)
✅ UserProfileController.cs (există)
🆕 RecipeController.cs
  - GET /api/recipes (+ filtering, pagination)
  - GET /api/recipes/{id}
  - POST /api/recipes (create)
  - PUT /api/recipes/{id} (update)
  - DELETE /api/recipes/{id}
  - GET /api/recipes/premium (doar cu subscripție)

🆕 MealPlanController.cs
  - GET /api/mealplans (user's meal plans)
  - GET /api/mealplans/{date} (plan pentru o zi)
  - POST /api/mealplans/daily (create daily plan)
  - POST /api/mealplans/weekly (create weekly plan)
  - PUT /api/mealplans/{id}
  - DELETE /api/mealplans/{id}

🆕 FoodPreferenceController.cs
  - GET /api/foodpreferences
  - POST /api/foodpreferences
  - PUT /api/foodpreferences

🆕 AllergyController.cs
  - GET /api/allergies
  - POST /api/allergies
  - DELETE /api/allergies/{id}

🆕 SubscriptionController.cs
  - GET /api/subscription/status
  - POST /api/subscription/upgrade
  - POST /api/subscription/cancel
  - POST /api/subscription/payment

🆕 AIController.cs
  - POST /api/ai/recommend-recipes (body: user profile + preferences)
  - POST /api/ai/generate-meal-plan (body: duration, preferences)
  - GET /api/ai/recommendations/{userId}
  - POST /api/ai/coach (AI nutritionist chat)
```

---

## FAZA 2: Modul Python AI ⏱️ 1-2 săptămâni

### 2.1 Setup Proiect Python

**Prioritate: CRITICAL**

Creează un proiect nou în `/PythonAI/`:

```
Opțiuni:
1. FastAPI (recomandat pentru simplitate)
2. Django + Django REST Framework
```

**Structură proiect FastAPI:**

```
PythonAI/
  app/
    main.py (entrypoint)
    config.py (settings)
    
    models/
      user_profile.py (Pydantic models)
      recipe.py
      recommendation.py
    
    services/
      recommendation_engine.py (logica AI)
      meal_plan_generator.py
      nutritionist_coach.py (chatbot)
    
    api/
      routes/
        recommendations.py
        meal_plans.py
        coach.py
    
    ml/
      models/ (modele antrenate)
      preprocessing.py
      training.py (optional)
    
    utils/
      database.py (dacă e nevoie)
      http_client.py (pentru .NET API)
  
  requirements.txt
  Dockerfile
  .env
  pytest.ini
  tests/
```

### 2.2 Modelul AI de Recomandare

**Prioritate: HIGH**

**Tehnologii sugerate:**
- **Scikit-learn** pentru modele simple (clustering, classification)
- **TensorFlow/PyTorch** pentru deep learning (opțional)
- **Pandas** pentru procesare date
- **NumPy** pentru calcule numerice

**Funcționalități:**

1. **Recipe Recommendation System**:
   - Input: User profile (age, weight, height, objective, allergies, preferences)
   - Output: Top N recipes recomandate
   - Algoritm: Collaborative filtering sau content-based filtering

2. **Meal Plan Generator**:
   - Input: User profile + duration (zilnic/săptămânal)
   - Output: Meal plan complet cu breakfast/lunch/dinner/snacks
   - Constrângeri: target calories, macros, allergies, preferences

3. **AI Nutritionist Coach** (chatbot):
   - Input: User question (text)
   - Output: Răspuns personalizat
   - Tehnologie: OpenAI API (GPT-4) sau un model local (LLaMA, etc.)

**Exemple endpoint-uri FastAPI:**

```python
# POST /api/recommendations/recipes
@app.post("/api/recommendations/recipes")
async def recommend_recipes(profile: UserProfile):
    # Logică AI
    return {"recipes": [...]}

# POST /api/recommendations/meal-plan
@app.post("/api/recommendations/meal-plan")
async def generate_meal_plan(request: MealPlanRequest):
    # Logică AI
    return {"meal_plan": {...}}

# POST /api/coach/ask
@app.post("/api/coach/ask")
async def ai_coach(question: CoachQuestion):
    # Chatbot
    return {"answer": "..."}
```

### 2.3 Integrare .NET ↔ Python

**Prioritate: HIGH**

În `.NET Infrastructure/ExternalServices/PythonAIService.cs`:

```csharp
public class PythonAIService
{
    private readonly HttpClient _httpClient;
    
    public async Task<List<RecipeDto>> GetRecommendedRecipes(UserProfile profile)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "http://python-ai:8000/api/recommendations/recipes", 
            profile
        );
        return await response.Content.ReadFromJsonAsync<List<RecipeDto>>();
    }
    
    public async Task<MealPlanDto> GenerateMealPlan(MealPlanRequest request)
    {
        // Similar
    }
}
```

---

## FAZA 3: Sistem de Abonament Premium ⏱️ 3-5 zile

### 3.1 Implementare Backend

**Prioritate: MEDIUM-HIGH**

**Features:**
- Utilizatori Free: acces la rețete de bază, planuri manuale
- Utilizatori Premium: rețete premium, planuri AI generate, AI coach

**Middleware pentru autorizare:**

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePremiumAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user has active premium subscription
    }
}
```

**Integrare plată** (opțional pentru demo):
- Stripe API
- PayPal
- Sau simplu set manual în DB pentru început

### 3.2 UI în Blazor

**Prioritate: MEDIUM**

Pagini noi:
- `/subscription` - status abonament
- `/upgrade` - upgrade la Premium
- Restricții pe rețete premium (show lock icon pentru Free users)

---

## FAZA 4: Interfață Blazor WebAssembly Completă ⏱️ 1 săptămână

### 4.1 Pagini noi în Frontend

**Prioritate: HIGH**

```
Frontend/Pages/
  ✅ Home.razor (există)
  ✅ Login.razor (există)
  ✅ Register.razor (există)
  ✅ Profil.razor (există)
  🆕 Dashboard.razor (dashboard cu grafice)
  
  🆕 Recipes/
    - RecipesList.razor (CRUD recipes)
    - RecipeDetails.razor
    - CreateRecipe.razor
    - EditRecipe.razor
  
  🆕 MealPlans/
    - MealPlansList.razor
    - CreateMealPlan.razor
    - WeeklyView.razor (calendar view)
  
  🆕 FoodPreferences.razor
  🆕 Allergies.razor
  
  🆕 AIRecommendations.razor (AI suggested recipes)
  🆕 AICoach.razor (chat interface cu AI nutritionist)
  
  🆕 Subscription/
    - SubscriptionStatus.razor
    - Upgrade.razor
```

### 4.2 State Management

**Prioritate: MEDIUM**

Opțiuni:
1. **Fluxor** (Redux pattern pentru Blazor)
2. **Mediatr** pattern local
3. Simple state container

### 4.3 UI Components

**Prioritate: MEDIUM**

- Dashboard cu charts (folosește Blazor Charts sau ApexCharts)
- Calendar component pentru meal plans
- Recipe card component
- Chat interface pentru AI coach
- Subscription upgrade modal

---

## FAZA 5: Containerizare Docker ⏱️ 2-3 zile

### 5.1 Dockerfiles

**Prioritate: HIGH**

**1. Frontend Dockerfile** (`Frontend/Dockerfile`):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Frontend/Frontend.csproj", "Frontend/"]
RUN dotnet restore "Frontend/Frontend.csproj"
COPY Frontend/ Frontend/
WORKDIR "/src/Frontend"
RUN dotnet build "Frontend.csproj" -c Release -o /app/build
RUN dotnet publish "Frontend.csproj" -c Release -o /app/publish

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY Frontend/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
```

**2. Backend API Dockerfile** (`NutritionAdvisor.API/Dockerfile`):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NutritionAdvisor.API/NutritionAdvisor.API.csproj", "NutritionAdvisor.API/"]
COPY ["NutritionAdvisor.Application/NutritionAdvisor.Application.csproj", "NutritionAdvisor.Application/"]
COPY ["NutritionAdvisor.Domain/NutritionAdvisor.Domain.csproj", "NutritionAdvisor.Domain/"]
COPY ["NutritionAdvisor.Infrastructure/NutritionAdvisor.Infrastructure.csproj", "NutritionAdvisor.Infrastructure/"]

RUN dotnet restore "NutritionAdvisor.API/NutritionAdvisor.API.csproj"
COPY . .
WORKDIR "/src/NutritionAdvisor.API"
RUN dotnet build "NutritionAdvisor.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NutritionAdvisor.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NutritionAdvisor.API.dll"]
```

**3. Python AI Dockerfile** (`PythonAI/Dockerfile`):

```dockerfile
FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app/ ./app/

EXPOSE 8000

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

### 5.2 Docker Compose

**Prioritate: HIGH**

`docker-compose.yml` în root:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: nutrition_user
      POSTGRES_PASSWORD: nutrition_pass
      POSTGRES_DB: nutrition_db
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - nutrition-network

  backend:
    build:
      context: .
      dockerfile: NutritionAdvisor.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=nutrition_db;Username=nutrition_user;Password=nutrition_pass
      - Jwt__Key=${JWT_KEY}
      - Jwt__Issuer=NutritionAdvisor
      - Jwt__Audience=NutritionAdvisor
      - PythonAI__BaseUrl=http://python-ai:8000
    ports:
      - "5066:8080"
    depends_on:
      - postgres
    networks:
      - nutrition-network

  python-ai:
    build:
      context: ./PythonAI
      dockerfile: Dockerfile
    environment:
      - DATABASE_URL=postgresql://nutrition_user:nutrition_pass@postgres:5432/nutrition_db
      - DOTNET_API_URL=http://backend:8080
    ports:
      - "8000:8000"
    depends_on:
      - postgres
    networks:
      - nutrition-network

  frontend:
    build:
      context: .
      dockerfile: Frontend/Dockerfile
    ports:
      - "5210:80"
    environment:
      - ApiBaseUrl=http://backend:8080
    depends_on:
      - backend
    networks:
      - nutrition-network

volumes:
  postgres_data:

networks:
  nutrition-network:
    driver: bridge
```

### 5.3 Registry & Push

**Prioritate: MEDIUM**

```bash
# Build images
docker build -t nutrition-backend:latest -f NutritionAdvisor.API/Dockerfile .
docker build -t nutrition-frontend:latest -f Frontend/Dockerfile .
docker build -t nutrition-ai:latest -f PythonAI/Dockerfile ./PythonAI

# Tag pentru registry (ex: Docker Hub)
docker tag nutrition-backend:latest yourusername/nutrition-backend:latest
docker tag nutrition-frontend:latest yourusername/nutrition-frontend:latest
docker tag nutrition-ai:latest yourusername/nutrition-ai:latest

# Push
docker push yourusername/nutrition-backend:latest
docker push yourusername/nutrition-frontend:latest
docker push yourusername/nutrition-ai:latest
```

---

## FAZA 6: Teste - Unit & Integration ⏱️ 1 săptămână

### 6.1 Unit Tests (.NET)

**Prioritate: CRITICAL pentru bonus**

Proiect nou: `NutritionAdvisor.Tests/`

```
NutritionAdvisor.Tests/
  UnitTests/
    Application/
      Commands/
        Recipes/
          CreateRecipeCommandHandlerTests.cs
        MealPlans/
          CreateMealPlanCommandHandlerTests.cs
      Queries/
        Recipes/
          GetAllRecipesQueryHandlerTests.cs
    
    Domain/
      Entities/
        RecipeTests.cs
        UserProfileTests.cs
  
  IntegrationTests/
    API/
      RecipeControllerTests.cs
      MealPlanControllerTests.cs
      AuthControllerTests.cs
    
    Infrastructure/
      Repositories/
        RecipeRepositoryTests.cs
```

**Librării:**
- xUnit sau NUnit
- Moq (mocking)
- FluentAssertions
- Testcontainers (pentru integration tests cu PostgreSQL real)

**Exemplu Unit Test:**

```csharp
public class CreateRecipeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateRecipe()
    {
        // Arrange
        var mockRepo = new Mock<IRecipeRepository>();
        var handler = new CreateRecipeCommandHandler(mockRepo.Object);
        var command = new CreateRecipeCommand { Title = "Test Recipe", ... };
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.Should().NotBeEmpty();
        mockRepo.Verify(x => x.AddAsync(It.IsAny<Recipe>()), Times.Once);
    }
}
```

**Target: 60-70% code coverage**

### 6.2 Integration Tests (.NET)

**Prioritate: HIGH**

Cu **Testcontainers**:

```csharp
public class RecipeControllerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    
    public RecipeControllerIntegrationTests()
    {
        _postgresContainer = new PostgreSqlBuilder().Build();
    }
    
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }
    
    [Fact]
    public async Task GetRecipes_ShouldReturnRecipes()
    {
        // Arrange: WebApplicationFactory cu DB din container
        // Act: HTTP request
        // Assert
    }
}
```

### 6.3 Python Tests

**Prioritate: MEDIUM-HIGH**

În `PythonAI/tests/`:

```
tests/
  unit/
    test_recommendation_engine.py
    test_meal_plan_generator.py
  
  integration/
    test_api_endpoints.py
```

**Pytest example:**

```python
# test_recommendation_engine.py
import pytest
from app.services.recommendation_engine import RecommendationEngine

def test_recommend_recipes_for_user():
    engine = RecommendationEngine()
    user_profile = {...}
    
    recipes = engine.recommend_recipes(user_profile, top_n=5)
    
    assert len(recipes) == 5
    assert all(r.calories > 0 for r in recipes)
```

**Librării:**
- pytest
- pytest-asyncio
- httpx (pentru test API calls)
- pytest-cov (coverage)

---

## FAZA 7: SonarQube / SonarCloud ⏱️ 1-2 zile

### 7.1 Setup SonarCloud

**Prioritate: MEDIUM (requir pentru bonus)**

1. Creează cont pe [sonarcloud.io](https://sonarcloud.io/)
2. Conectează repo GitHub/GitLab
3. Obține token

### 7.2 Integrare în .NET

**În .NET API project:**

Instalează SonarScanner:

```bash
dotnet tool install --global dotnet-sonarscanner
```

Adaugă în pipeline (vezi Faza 8):

```bash
dotnet sonarscanner begin /k:"your-project-key" /o:"your-org" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="YOUR_TOKEN"
dotnet build
dotnet test --collect:"XPlat Code Coverage"
dotnet sonarscanner end /d:sonar.login="YOUR_TOKEN"
```

### 7.3 Python

**SonarCloud pentru Python:**

Creează `sonar-project.properties`:

```properties
sonar.projectKey=nutrition-advisor-ai
sonar.organization=your-org
sonar.sources=app
sonar.tests=tests
sonar.python.coverage.reportPaths=coverage.xml
```

Run:

```bash
pytest --cov=app --cov-report=xml
sonar-scanner
```

---

## FAZA 8: CI/CD Pipeline ⏱️ 2-3 zile

### 8.1 GitHub Actions Workflow

**Prioritate: HIGH**

Creează `.github/workflows/ci-cd.yml`:

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Run tests
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
      - name: SonarCloud Scan
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        run: |
          dotnet sonarscanner begin /k:"nutrition-backend" /o:"your-org" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="${SONAR_TOKEN}"
          dotnet build
          dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}"

  test-python:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Python
        uses: actions/setup-python@v5
        with:
          python-version: '3.11'
      
      - name: Install dependencies
        working-directory: ./PythonAI
        run: |
          pip install -r requirements.txt
          pip install pytest pytest-cov
      
      - name: Run tests
        working-directory: ./PythonAI
        run: pytest --cov=app --cov-report=xml
      
      - name: SonarCloud Scan
        uses: SonarSource/sonarcloud-github-action@master
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        with:
          projectBaseDir: PythonAI

  build-and-push-images:
    needs: [test-backend, test-python]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4
      
      - name: Login to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_USERNAME }}
          password: ${{ secrets.DOCKER_PASSWORD }}
      
      - name: Build and push Backend
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./NutritionAdvisor.API/Dockerfile
          push: true
          tags: ${{ secrets.DOCKER_USERNAME }}/nutrition-backend:latest
      
      - name: Build and push Frontend
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./Frontend/Dockerfile
          push: true
          tags: ${{ secrets.DOCKER_USERNAME }}/nutrition-frontend:latest
      
      - name: Build and push Python AI
        uses: docker/build-push-action@v5
        with:
          context: ./PythonAI
          file: ./PythonAI/Dockerfile
          push: true
          tags: ${{ secrets.DOCKER_USERNAME }}/nutrition-ai:latest

  deploy-to-cloud:
    needs: build-and-push-images
    runs-on: ubuntu-latest
    steps:
      - name: Deploy to Azure/AWS/GCP
        run: |
          # Comenzi deploy specifice platformei
          # Exemplu pentru Azure Container Apps:
          # az containerapp update ...
```

### 8.2 Azure DevOps (alternativă)

Creează `azure-pipelines.yml` cu structură similară.

### 8.3 Quality Gates

**În SonarCloud:**
- Coverage > 60%
- 0 Vulnerabilities
- 0 Bugs (Critical/Blocker)
- Code Smells < 5%

---

## FAZA 9: Cloud Deployment ⏱️ 2-3 zile

### 9.1 Alegerea Platformei

**Opțiuni:**

#### **Opțiunea A: Azure** (recomandat pentru .NET)

**Servicii:**
- **Azure Container Apps** sau **App Service** pentru Backend & AI
- **Azure Static Web Apps** pentru Blazor WASM
- **Azure Database for PostgreSQL**
- **Azure Blob Storage** pentru imagini rețete
- **Azure Container Registry** (ACR) pentru images

**Setup:**

```bash
# Login
az login

# Create resource group
az group create --name nutrition-rg --location eastus

# Create Container Registry
az acr create --resource-group nutrition-rg --name nutritionacr --sku Basic

# Create PostgreSQL
az postgres flexible-server create \
  --resource-group nutrition-rg \
  --name nutrition-db-server \
  --admin-user adminuser \
  --admin-password YourPassword123!

# Deploy Backend to Container App
az containerapp create \
  --name nutrition-backend \
  --resource-group nutrition-rg \
  --image nutritionacr.azurecr.io/nutrition-backend:latest \
  --target-port 8080 \
  --ingress external \
  --environment nutrition-env

# Deploy Static Web App (Blazor)
az staticwebapp create \
  --name nutrition-frontend \
  --resource-group nutrition-rg \
  --source https://github.com/yourusername/nutrition-advisor \
  --location eastus \
  --branch main \
  --app-location "/Frontend" \
  --output-location "wwwroot"
```

#### **Opțiunea B: AWS**

**Servicii:**
- **AWS ECS** (Elastic Container Service) pentru backend & AI
- **AWS RDS PostgreSQL**
- **AWS S3** + **CloudFront** pentru frontend static
- **AWS ECR** pentru container registry

#### **Opțiunea C: GCP**

**Servicii:**
- **Cloud Run** pentru backend & AI
- **Cloud SQL PostgreSQL**
- **Cloud Storage** + **Cloud CDN** pentru frontend
- **Artifact Registry** pentru containers

### 9.2 Environment Variables & Secrets

**Azure Key Vault** sau **AWS Secrets Manager**:

- JWT Secret Key
- Database connection strings
- OpenAI API key (dacă folosești)
- Payment gateway keys

### 9.3 Monitoring & Logging

**Azure:**
- Application Insights
- Log Analytics

**AWS:**
- CloudWatch

**GCP:**
- Cloud Monitoring
- Cloud Logging

---

## FAZA 10: Finalizare & Polish ⏱️ 2-3 zile

### 10.1 Documentation

**Prioritate: MEDIUM**

Creează:
- **README.md** complet cu setup instructions
- **API Documentation** (Swagger/OpenAPI)
- **Architecture Diagrams** (C4 model sau similar)
- **User Guide** pentru interfața web

### 10.2 Performance Optimization

- Caching (Redis) pentru queries frecvente
- Database indexing
- API rate limiting
- Frontend lazy loading

### 10.3 Security Hardening

- HTTPS everywhere
- CORS policies
- Input validation
- SQL injection prevention (EF Core face asta)
- XSS protection
- Rate limiting per user

### 10.4 Final Testing

- End-to-end testing (Playwright sau Selenium)
- Load testing (K6, JMeter)
- Security scanning (OWASP ZAP)

---

## 📅 Timeline Estimat Total: 6-8 săptămâni

| Fază | Durata | Prioritate |
|------|--------|-----------|
| 1. Backend .NET extensii | 1-2 săpt | ⚠️ CRITICAL |
| 2. Python AI | 1-2 săpt | ⚠️ CRITICAL |
| 3. Sistem Premium | 3-5 zile | HIGH |
| 4. Frontend Blazor complet | 1 săpt | HIGH |
| 5. Docker | 2-3 zile | HIGH |
| 6. Teste | 1 săpt | ⚠️ CRITICAL |
| 7. SonarQube | 1-2 zile | MEDIUM |
| 8. CI/CD | 2-3 zile | HIGH |
| 9. Cloud Deploy | 2-3 zile | HIGH |
| 10. Polish | 2-3 zile | MEDIUM |

---

## ✅ Checklist Final pentru Bonus Complet

### Clean Architecture ✔️
- [ ] Domain Layer complet (toate entitățile)
- [ ] Application Layer cu CQRS complet (Commands + Queries)
- [ ] Infrastructure Layer cu repositories
- [ ] API Layer cu controllers

### CQRS & MediatR ✔️
- [ ] Toate operațiile au Commands separate de Queries
- [ ] Validators cu FluentValidation
- [ ] Handlers pentru fiecare command/query

### Python AI Module ✔️
- [ ] FastAPI/Django setup
- [ ] Recommendation engine functional
- [ ] Meal plan generator
- [ ] AI Coach (chatbot)
- [ ] Integrare cu .NET API

### Blazor WASM ✔️
- [ ] Interfață completă și responsivă
- [ ] Autentificare + Autorizare
- [ ] Dashboard cu grafice
- [ ] CRUD pentru entități
- [ ] State management
- [ ] UI modern (MudBlazor)

### Docker ✔️
- [ ] Dockerfile pentru Backend
- [ ] Dockerfile pentru Frontend
- [ ] Dockerfile pentru Python AI
- [ ] docker-compose.yml functional
- [ ] Images în registry (Docker Hub/ACR/ECR)

### Cloud Deployment ✔️
- [ ] Backend deployed in cloud
- [ ] Frontend deployed in cloud
- [ ] Python AI deployed in cloud
- [ ] Database in cloud
- [ ] File storage in cloud

### Tests ✔️
- [ ] Unit tests pentru .NET (60-70% coverage)
- [ ] Integration tests pentru .NET
- [ ] Unit tests pentru Python
- [ ] Integration tests pentru Python

### SonarQube ✔️
- [ ] SonarCloud setup
- [ ] Quality Gates configured
- [ ] Coverage reports
- [ ] Zero critical bugs/vulnerabilities

### CI/CD ✔️
- [ ] Automated build
- [ ] Automated tests
- [ ] SonarQube scan in pipeline
- [ ] Docker build & push
- [ ] Automated deployment

### Calitate Cod ✔️
- [ ] SOLID principles
- [ ] Naming conventions
- [ ] DI (Dependency Injection)
- [ ] Error handling
- [ ] Logging

---

## 🎯 Pași Următori Imediați

1. **START: Extinde entitățile Domain** (Recipe, MealPlan, etc.)
2. **Crează structura CQRS pentru Recipes**
3. **Implementează RecipeController cu CRUD**
4. **Setup proiect Python FastAPI**
5. **Crează primul endpoint AI (recommend recipes)**
6. **Integrează .NET cu Python AI**
7. **Adaugă pagina Recipes în Blazor**
8. **Implementează primul test**
9. **Creează Dockerfiles**
10. **Setup CI/CD pipeline**

---

**Notă**: Acest roadmap este exhaustiv. Poți ajusta prioritățile în funcție de deadline și complexitatea dorită. Partea de AI poate fi simplificată inițial (algoritmi simpli) și îmbunătățită iterativ.

Succes! 🚀
