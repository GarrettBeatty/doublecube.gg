import SwiftUI

struct RegisterView: View {
    @StateObject private var vm = AuthViewModel()
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        VStack(spacing: 24) {
            Spacer()

            Text("Create Account")
                .font(.title.bold())

            VStack(spacing: 12) {
                TextField("Username", text: $vm.username)
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()
                    .textFieldStyle(.roundedBorder)

                TextField("Email (optional)", text: $vm.email)
                    .textInputAutocapitalization(.never)
                    .keyboardType(.emailAddress)
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

            Button {
                Task { await vm.register() }
            } label: {
                Group {
                    if vm.isLoading {
                        ProgressView()
                    } else {
                        Text("Register")
                    }
                }
                .frame(maxWidth: .infinity)
            }
            .buttonStyle(.borderedProminent)
            .padding(.horizontal)
            .disabled(vm.isLoading)

            Spacer()
        }
        .navigationTitle("Register")
        .onChange(of: AuthService.shared.currentUser) { _, user in
            if user != nil { dismiss() }
        }
    }
}

#Preview {
    NavigationStack { RegisterView() }
}
