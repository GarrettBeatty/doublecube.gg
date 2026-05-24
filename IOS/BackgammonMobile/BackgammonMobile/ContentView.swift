//
//  ContentView.swift
//  BackgammonMobile
//
//  Created by Garrett Beatty on 5/24/26.
//

import SwiftUI

struct ContentView: View {
    @EnvironmentObject private var auth: AuthService
    @ObservedObject private var hub = GameHubService.shared

    var body: some View {
        Group {
            if auth.currentUser != nil {
                HomeView()
                    .task {
                        // Connect to SignalR as soon as we have a token
                        if let token = auth.token, hub.connectionState == .disconnected {
                            try? await hub.connect(token: token)
                        }
                    }
            } else {
                LoginView()
            }
        }
        .animation(.default, value: auth.currentUser?.userId)
    }
}

#Preview {
    ContentView()
        .environmentObject(AuthService.shared)
}
