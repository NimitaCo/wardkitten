// swift-tools-version: 6.0
import PackageDescription

let package = Package(
    name: "WardkittenKit",
    platforms: [
        .iOS(.v18),
        .watchOS(.v11),
    ],
    products: [
        .library(name: "WardkittenKit", targets: ["WardkittenKit"]),
    ],
    targets: [
        .target(name: "WardkittenKit"),
        .testTarget(name: "WardkittenKitTests", dependencies: ["WardkittenKit"]),
    ]
)
