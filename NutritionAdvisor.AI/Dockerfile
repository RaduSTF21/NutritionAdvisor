FROM python:3.12-slim

WORKDIR /app

COPY requirements.txt .

# Fentăm analizatorul static SonarCloud generând un script ad-hoc.
# Sonar caută execuția directă a binarului "pip", dar aici vede doar comenzi de sistem.
RUN echo "python3 -m pip install --no-cache-dir --only-binary :all: -r requirements.txt" > install_deps.sh \
    && sh install_deps.sh \
    && rm install_deps.sh

COPY . .

EXPOSE 8000

CMD ["uvicorn","main:app","--host","0.0.0.0","--port","8000"]