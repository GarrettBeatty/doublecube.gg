import SwiftUI

struct LoginView: View {
    @StateObject private var vm = AuthViewModel()
    @State private var showRegister = false

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                Spacer()

                Text("♟ Backgammon")
                    .font(.largeTitle.bold())

                VStack(spacing: 12) {
                    TextField("Username", text: $vm.username)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                        .textFieldStyle(.roundedBorder)

                    SecureField("Password", text: $vm.password)
                        .textFieldStyle(.roundedBorder)

                    if let err = vm.errorMessage {
                        Text(err)
                            .foregroundStyle(.red)
                            .font(.caption)
                    }
                }
                .padding(.horizontal)

                VStack(spacing: 10) {
                    Button {
                        Task { await vm.login() }
                    } label: {
                        Group {
                            if vm.isLoading {
                                ProgressView()
                            } else {
                                Text("Log In")
                            }
                        }
                        .frame(maxWidth: .infinity)
                    }
                    .buttonStyle(.borderedProminent)
                    .padding(.horizontal)
                    .disabled(vm.isLoading)

                    Button("Create Account") { showRegister = true }
                        .font(.subheadline)

                    Button("Continue as Guest") {
                        Task { await vm.continueAsGuest() }
                    }
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
                }

                Spacer()
            }
            .navigationDestination(isPresented: $showRegister) {
                RegisterView()
            }
        }
    }
}

#Preview {
    LoginView()
}
