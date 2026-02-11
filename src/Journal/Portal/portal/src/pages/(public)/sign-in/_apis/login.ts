import { portalClient } from "@/api-client";
import { useMutation } from "@tanstack/react-query";

type LoginPayload = {
    account: string;
    password: string;
};

type LoginResponse = {
    token: string;
};  

export async function postLogin(payload: LoginPayload): Promise<LoginResponse> {
    const response = await portalClient.post("/authentication/login", payload);
    if (response.status !== 201) {
        throw new Error(`Error logging in: ${response.statusText}`);
    }
    return response.data;
}   

export function useLogin() {
    return useMutation({
        mutationFn: postLogin,
        mutationKey: ["login"],
    });
}