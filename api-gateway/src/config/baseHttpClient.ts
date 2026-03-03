import axios from "axios";
import { appConfig } from "./settings";

export const httpClient = axios.create({
    baseURL: appConfig.API_CORE_URL,
    headers: {
        "Content-Type": "application/json",
    },
    timeout: 10000,
});

import { HttpException, HttpStatus } from "@nestjs/common";

httpClient.interceptors.response.use(
    (response) => response.data,
    (error) => {
        const status = error.response?.status || 500;
        const data = error.response?.data;
        const rawMessage: string = data?.message || data?.detail || (typeof data === 'string' ? data : '') || error.message || "Internal Server Error";

        if (
            rawMessage.includes("IDX10223") ||
            rawMessage.toLowerCase().includes("lifetime validation failed") ||
            rawMessage.toLowerCase().includes("token is expired")
        ) {
            throw new HttpException(
                {
                    statusCode: HttpStatus.UNAUTHORIZED,
                    errorCode: "TOKEN_EXPIRED",
                    message: "Your session has expired. Please log in again.",
                },
                HttpStatus.UNAUTHORIZED
            );
        }

        // Detect generic .NET 401 Unauthorized (e.g., missing Bearer)
        if (status === 401) {
            throw new HttpException(
                {
                    statusCode: HttpStatus.UNAUTHORIZED,
                    errorCode: "UNAUTHORIZED",
                    message: rawMessage || "Unauthorized. Please provide a valid token.",
                },
                HttpStatus.UNAUTHORIZED
            );
        }

        throw new HttpException(
            {
                statusCode: status,
                message: rawMessage,
            },
            status
        );
    }
);

export default httpClient;