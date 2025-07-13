package com.aegis.messenger.network

import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.*

interface ApiService {
    @POST("/keys")
    suspend fun publishPreKeys(@Body body: Map<String, Any>): ApiResponse

    @GET("/keys/{userId}")
    suspend fun getPreKeys(@Path("userId") userId: String): ApiResponse

    @POST("/messages")
    suspend fun sendMessage(@Body body: Map<String, Any>): ApiResponse
}

object NetworkClient {
    private val retrofit = Retrofit.Builder()
        .baseUrl("https://your-server-url.com/api/")
        .addConverterFactory(GsonConverterFactory.create())
        .build()
    val api: ApiService = retrofit.create(ApiService::class.java)

    // WebSocket
    fun createWebSocket(listener: WebSocketListener): WebSocket {
        val client = OkHttpClient()
        val request = Request.Builder().url("wss://your-server-url.com/ws").build()
        return client.newWebSocket(request, listener)
    }
}

class ApiResponse(val success: Boolean, val data: Any?)
