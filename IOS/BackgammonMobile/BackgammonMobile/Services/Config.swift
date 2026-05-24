import Foundation

enum Config {
    /// Change to your server address for device testing.
    /// Use http://localhost:5000 for Simulator, or your machine's LAN IP for a real device.
    static let serverURL = "http://localhost:5000"
    static let hubURL = "\(serverURL)/gamehub"
}
