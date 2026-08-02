import Testing
import Foundation
@testable import WardkittenKit

struct WatchTests {
    @Test func decodesFromApiPayload() throws {
        let json = #"{"id":"abc","name":"Backup nocturno","status":"Ok","criticality":"High"}"#
        let watch = try JSONDecoder().decode(Watch.self, from: Data(json.utf8))
        #expect(watch.id == "abc")
        #expect(watch.status == .ok)
    }
}
