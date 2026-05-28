# ── Stage 1: Build ────────────────────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files first — cached until package.json changes
COPY package.json package-lock.json ./
RUN npm ci --silent

# Copy source and build
COPY . .

# API URL is baked into the bundle at build time
# Overridden by --build-arg in docker compose
ARG VITE_API_URL=https://localhost:5100/api
ENV VITE_API_URL=$VITE_API_URL

RUN npm run build

# ── Stage 2: Serve with Nginx ─────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Remove default nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy our nginx config
COPY nginx.conf /etc/nginx/conf.d/

# Copy built static files from build stage
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]