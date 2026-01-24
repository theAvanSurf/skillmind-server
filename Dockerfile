# Use Node.js 20 Alpine
FROM node:20-alpine

# Set working directory
WORKDIR /usr/src/app

# Copy dependency files
COPY package*.json pnpm-lock.yaml* ./

# Install dependencies
RUN npm install -g pnpm && pnpm install

# Copy source code
COPY . .

# Build NestJS app
RUN pnpm run build

# Expose API Gateway port
EXPOSE 3000

# Start the app
CMD ["node", "dist/main.js"]
