//
//  BackgammonMobileApp.swift
//  BackgammonMobile
//
//  Created by Garrett Beatty on 5/24/26.
//

import SwiftUI

@main
struct BackgammonMobileApp: App {
    @StateObject private var auth = AuthService.shared

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(auth)
                .task { await auth.restoreSession() }
        }
    }
}
