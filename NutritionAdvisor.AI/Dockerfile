FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt .
# NOSONAR docker:S8544Beta docker:S8541Beta
RUN pip install --no-cache-dir --only-binary :all: -r requirements.txt || pip install --no-cache-dir -r requirements.txt

COPY . .

EXPOSE 8000

CMD ["uvicorn","main:app","--host","0.0.0.0","--port","8000"]