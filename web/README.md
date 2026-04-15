# Трекер задач «Обезьяна и Умник» — Frontend

Фронтенд-часть веб-приложения для ежедневного планирования.  
Реализована на **Spring MVC + Thymeleaf** с использованием **Tailwind CSS**.

---

## 📋 О проекте

Приложение помогает пользователю каждый день понимать, **что делать прямо сейчас**, и каждую неделю — **куда он движется
**.

Модель построена на двух когнитивных режимах:

- 🐒 **Обезьяна** — исполнитель, берёт задачи из инбокса и делает
- 🧠 **Умник** — планировщик, декомпозирует проекты, проводит ревью, синхронизирует векторы

---

## 🚀 Быстрый старт

### 1. Установите Java JDK 17

| Компонент    | Версия       | Ссылка для скачивания                                                                                        |
|--------------|--------------|--------------------------------------------------------------------------------------------------------------|
| **Java JDK** | 17 или новее | [Eclipse Temurin](https://adoptium.net/) / [Oracle JDK](https://www.oracle.com/java/technologies/downloads/) |

Проверьте установку:

```bash
java -version   # Должно показать Java 17+
javac -version  # Должно показать javac 17+
```

### 2. Запустите приложение

У вас есть два варианта:

**Вариант 1: Через глобальный Maven (если установлен)**
Скачать Maven: https://maven.apache.org/download.cgi

```bash
mvn spring-boot:run
```

**Вариант 2: Через Maven Wrapper (Maven не требуется)**

```bash
# Windows
.\mvnw.cmd spring-boot:run

# Mac / Linux
./mvnw spring-boot:run
```

> 💡 Maven Wrapper уже включён в репозиторий — никакой дополнительной установки не нужно!

### 3.Откройте в браузере

```text
http://localhost:8080
```

---

## ⚙️ Конфигурация

**Изменение порта (опционально)**
В файле src/main/resources/application.yml:

```yaml
server:
  port: 8080   # Поменяйте на любой другой порт
```

## 🧹 Остановка сервера

В терминале нажмите:

```text
Ctrl + C
```

## 🧹 Форматирование кода (Frontend)
Мы используем плагин **Spotless** (стандарт Google Java Format). Чтобы сборка не упала в GitHub Actions, перед пушем выполните команду:

```bash
cd web
./mvnw spotless:apply
```

## 📁 Структура проекта

```text
web/
├── .mvn/                          # Maven Wrapper конфигурация
│   └── wrapper/
│       ├── maven-wrapper.jar
│       └── maven-wrapper.properties
├── src/
│   └── main/
│       ├── java/
│       │   └── com/
│       │       └── jedi/
│       │           └── tasktracker/
│       │               ├── TaskTrackerApplication.java      # Точка входа
│       │               ├── config/                          # Конфиги
│       │               ├── client/                          # REST-клиент
│       │               └── controller/
│       │                   └── TaskController.java          # Контроллер
│       └── resources/
│           ├── templates/
│           │   └── tasks.html                               # Главная страница
│           └── application.yml                              # Конфигурация Spring
├── mvnw                           # Maven Wrapper (Mac/Linux)
├── mvnw.cmd                       # Maven Wrapper (Windows)
├── pom.xml                        # Зависимости Maven
└── .gitignore
```

---

## 🛠️ Технологии

| Технология         | Версия | Назначение               |
|--------------------|--------|--------------------------|
| Java               | 17     | Язык программирования    |
| Spring Boot        | 3.1.5  | Фреймворк                |
| Spring MVC         | -      | Веб-контроллеры          |
| Thymeleaf          | -      | Шаблонизатор HTML        |
| Tailwind CSS       | -      | Стилизация (CDN)         |
| Maven              | 3.8+   | Сборка проекта           |
| Spring Security    | -      | Авторизация              |
| GitHub OAuth2      | -      | Аутентификация           |
| Thymeleaf Security | -      | sec: атрибуты в шаблонах |

---

## ⚠️ Текущий статус

- Статический CRUD-шаблон
- Вёрстка на Tailwind CSS
- Адаптивный дизайн
- Maven Wrapper для кросс-платформенной сборки
- ~~Интеграция с C# REST API~~ → REST-клиент (заглушка, ожидает C# API)
- Авторизация через GitHub OAuth2

> 📌 **Важно:** Кнопки в демо-режиме (неактивны). Это статический макет, готовый для интеграции с бэкендом.
