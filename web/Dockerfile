FROM node:24-alpine AS build
WORKDIR /src
COPY package*.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

FROM nginx:stable-alpine-slim
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/web/browser /usr/share/nginx/html