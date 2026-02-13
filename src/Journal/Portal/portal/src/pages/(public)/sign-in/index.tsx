import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import * as z from "zod"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { LockIcon, MailIcon, AlertCircle } from "lucide-react"
import { useLogin } from "./_apis/login"

const loginSchema = z.object({
    account: z.string().email("Account không hợp lệ"),
    password: z.string().min(6, "Mật khẩu phải có ít nhất 6 ký tự"),
})

type LoginFormValues = z.infer<typeof loginSchema>

export default function Page() {
    const form = useForm<LoginFormValues>({
        resolver: zodResolver(loginSchema),
        defaultValues: {
            account: "",
            password: "",
        },
    })

    const { mutate: login, isPending, isError, error } = useLogin()

    const onSubmit = (data: LoginFormValues) => {
        login(data, {
            onSuccess: (response) => {
                // Lưu token vào localStorage
                localStorage.setItem("token", response.token)
                // TODO: Redirect to dashboard hoặc home page
                console.log("Login successful!", response)
                window.location.href = "/"
            },
            onError: (error) => {
                console.error("Login failed:", error)
            }
        })
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-background p-4">
            <Card className="w-full max-w-md">
                <CardHeader className="space-y-1">
                    <CardTitle className="text-2xl font-bold text-center">Sign In</CardTitle>
                    <CardDescription className="text-center">
                        Enter your account and password to sign in to the system
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <Form {...form}>
                        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                            {isError && (
                                <Alert variant="destructive">
                                    <AlertCircle className="h-4 w-4" />
                                    <AlertDescription>
                                        {error?.message || "Login failed. Please check your account and password."}
                                    </AlertDescription>
                                </Alert>
                            )}
                            <FormField
                                control={form.control}
                                name="account"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Account</FormLabel>
                                        <FormControl>
                                            <div className="relative">
                                                <MailIcon className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                                                <Input
                                                    placeholder="name@example.com"
                                                    type="email"
                                                    className="pl-10"
                                                    {...field}
                                                />
                                            </div>
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="password"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Password</FormLabel>
                                        <FormControl>
                                            <div className="relative">
                                                <LockIcon className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                                                <Input
                                                    placeholder="••••••••"
                                                    type="password"
                                                    className="pl-10"
                                                    {...field}
                                                />
                                            </div>
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <Button type="submit" className="w-full" disabled={isPending}>
                                {isPending ? "Processing..." : "Sign In"}
                            </Button>
                        </form>
                    </Form>
                    <div className="mt-6 text-center text-sm text-muted-foreground">
                        <p>Don't have an account?</p>
                        <p className="mt-1">
                            Please contact <span className="font-semibold text-foreground">Thiên Ngố</span> or <span className="font-semibold text-foreground">Việt Tồ</span> to request access.
                        </p>
                        <p className="mt-1 text-xs">
                            Admins will verify and notify you when your account is ready.
                        </p>
                    </div>
                </CardContent>
            </Card>
        </div>
    )
}