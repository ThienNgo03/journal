# Stage 1: build React app với Vite
FROM node:18 AS build
WORKDIR /app

# Copy package.json và cài dependencies
COPY package*.json ./
RUN npm install

# Copy toàn bộ source code
COPY . .

# Copy file .env.docker để dùng khi build
COPY .env .env

# Build ra thư mục dist
RUN npm run build

# Stage 2: serve bằng nginx
FROM nginx:alpine

# Copy kết quả build sang nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Copy cấu hình nginx để fallback về index.html cho React Router
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]