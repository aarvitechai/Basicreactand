# syntax=docker/dockerfile:1

# ---- Build ----
FROM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ARG VITE_API_BASE_URL=""
ARG VITE_APP_NAME="Red Star Vibe"
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL VITE_APP_NAME=$VITE_APP_NAME
RUN npm run build

# ---- Runtime ----
FROM nginx:1.29-alpine AS runtime
# API_UPSTREAM is substituted into the nginx template at container start.
ENV API_UPSTREAM=http://backend:8080
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
