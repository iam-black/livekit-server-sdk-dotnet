using Google.Protobuf;

namespace Livekit.Server.Sdk.Dotnet.Test
{
    [Collection("Integration tests")]
    public class RoomServiceClientTest : IAsyncLifetime
    {
        private ServiceClientFixture fixture;

        public RoomServiceClientTest(ServiceClientFixture fixture)
        {
            this.fixture = fixture;
        }

        private RoomServiceClient client = new RoomServiceClient(
            ServiceClientFixture.TEST_HTTP_URL,
            ServiceClientFixture.TEST_API_KEY,
            ServiceClientFixture.TEST_API_SECRET
        );

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Create_Room()
        {
            var request = new CreateRoomRequest
            {
                Name = TestConstants.ROOM_NAME,
                Metadata = TestConstants.ROOM_METADATA,
            };
            var room = await client.CreateRoom(request);
            Assert.NotNull(room);
            Assert.NotEmpty(room.Sid);
            Assert.Equal(TestConstants.ROOM_NAME, room.Name);
            Assert.Equal(TestConstants.ROOM_METADATA, room.Metadata);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task List_Rooms()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            var request = new ListRoomsRequest();
            var response = await client.ListRooms(request);
            Assert.NotNull(response);
            Assert.NotEmpty(response.Rooms);
            Assert.Contains(response.Rooms, r => r.Name == TestConstants.ROOM_NAME);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Delete_Room()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await client.DeleteRoom(new DeleteRoomRequest { Room = TestConstants.ROOM_NAME });
            var rooms = await client.ListRooms(new ListRoomsRequest());
            Assert.NotNull(rooms.Rooms);
            Assert.DoesNotContain(rooms.Rooms, r => r.Name == TestConstants.ROOM_NAME);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Update_RoomMetadata()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            var request = new UpdateRoomMetadataRequest
            {
                Room = TestConstants.ROOM_NAME,
                Metadata = "new-metadata",
            };
            Room room = await client.UpdateRoomMetadata(request);
            Assert.NotNull(room);
            Assert.Equal("new-metadata", room.Metadata);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task List_Participants()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            var request = new ListParticipantsRequest { Room = TestConstants.ROOM_NAME };
            var response = await client.ListParticipants(request);
            Assert.NotNull(response);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Get_Participant()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await fixture.JoinParticipant(
                TestConstants.ROOM_NAME,
                TestConstants.PARTICIPANT_IDENTITY
            );
            ParticipantInfo participant = await client.GetParticipant(
                new RoomParticipantIdentity
                {
                    Room = TestConstants.ROOM_NAME,
                    Identity = TestConstants.PARTICIPANT_IDENTITY,
                }
            );
            Assert.NotNull(participant);
            Assert.Equal(TestConstants.PARTICIPANT_IDENTITY, participant.Identity);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Remove_Participant()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await fixture.JoinParticipant(
                TestConstants.ROOM_NAME,
                TestConstants.PARTICIPANT_IDENTITY
            );
            var participants = await client.ListParticipants(
                new ListParticipantsRequest { Room = TestConstants.ROOM_NAME }
            );
            Assert.Contains(
                participants.Participants,
                p => p.Identity == TestConstants.PARTICIPANT_IDENTITY
            );
            var request = new RoomParticipantIdentity
            {
                Room = TestConstants.ROOM_NAME,
                Identity = TestConstants.PARTICIPANT_IDENTITY,
            };
            await client.RemoveParticipant(request);
            participants = await client.ListParticipants(
                new ListParticipantsRequest { Room = TestConstants.ROOM_NAME }
            );
            Assert.DoesNotContain(
                participants.Participants,
                p => p.Identity == TestConstants.PARTICIPANT_IDENTITY
            );
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Update_Participant()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await fixture.JoinParticipant(
                TestConstants.ROOM_NAME,
                TestConstants.PARTICIPANT_IDENTITY
            );
            ParticipantInfo participant = await client.GetParticipant(
                new RoomParticipantIdentity
                {
                    Room = TestConstants.ROOM_NAME,
                    Identity = TestConstants.PARTICIPANT_IDENTITY,
                }
            );
            Assert.True(participant.Permission.CanPublish);
            Assert.True(participant.Permission.CanSubscribe);
            Assert.False(participant.Permission.CanUpdateMetadata);
            var updateParticipantRequest = new UpdateParticipantRequest
            {
                Room = TestConstants.ROOM_NAME,
                Identity = TestConstants.PARTICIPANT_IDENTITY,
                Name = "new-name",
                Metadata = "new-metadata",
                Permission = new ParticipantPermission
                {
                    CanPublish = false,
                    CanSubscribe = false,
                    CanUpdateMetadata = true,
                },
            };
            participant = await client.UpdateParticipant(updateParticipantRequest);
            Assert.NotNull(participant);
            Assert.Equal(TestConstants.PARTICIPANT_IDENTITY, participant.Identity);
            Assert.Equal("new-name", participant.Name);
            Assert.Equal("new-metadata", participant.Metadata);
            Assert.False(participant.Permission.CanPublish);
            Assert.False(participant.Permission.CanSubscribe);
            Assert.True(participant.Permission.CanUpdateMetadata);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Mute_Published_Track()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await fixture.PublishVideoTrackInRoom(
                client,
                TestConstants.ROOM_NAME,
                TestConstants.PARTICIPANT_IDENTITY
            );
            var participant = await client.GetParticipant(
                new RoomParticipantIdentity
                {
                    Room = TestConstants.ROOM_NAME,
                    Identity = TestConstants.PARTICIPANT_IDENTITY,
                }
            );
            Assert.NotNull(participant);
            Assert.Single(participant.Tracks);
            Assert.False(participant.Tracks[0].Muted);
            var mutePublishedTrackRequest = new MuteRoomTrackRequest
            {
                Room = TestConstants.ROOM_NAME,
                Identity = TestConstants.PARTICIPANT_IDENTITY,
                TrackSid = participant.Tracks[0].Sid,
                Muted = true,
            };
            MuteRoomTrackResponse mutedPublishedTrack = await client.MutePublishedTrack(
                mutePublishedTrackRequest
            );
            Assert.NotNull(mutedPublishedTrack);
            Assert.True(mutedPublishedTrack.Track.Muted);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Update_Subscriptions()
        {
            string publisherIdentity = TestConstants.PARTICIPANT_IDENTITY + "2";
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await Task.WhenAll(
                fixture.JoinParticipant(
                    TestConstants.ROOM_NAME,
                    TestConstants.PARTICIPANT_IDENTITY
                ),
                fixture.PublishVideoTrackInRoom(client, TestConstants.ROOM_NAME, publisherIdentity)
            );
            var participants = await Task.WhenAll(
                client.GetParticipant(
                    new RoomParticipantIdentity
                    {
                        Room = TestConstants.ROOM_NAME,
                        Identity = TestConstants.PARTICIPANT_IDENTITY,
                    }
                ),
                client.GetParticipant(
                    new RoomParticipantIdentity
                    {
                        Room = TestConstants.ROOM_NAME,
                        Identity = publisherIdentity,
                    }
                )
            );
            var subscriber = participants[0];
            var publisher = participants[1];

            Assert.Single(publisher.Tracks);
            // Subscribe to track
            var updateSubscriptionsRequest = new UpdateSubscriptionsRequest
            {
                Room = TestConstants.ROOM_NAME,
                Identity = subscriber.Identity,
                Subscribe = true,
            };
            updateSubscriptionsRequest.TrackSids.Add(publisher.Tracks[0].Sid);
            await client.UpdateSubscriptions(updateSubscriptionsRequest);
            // Unsubscribe from track
            updateSubscriptionsRequest.Subscribe = false;
            updateSubscriptionsRequest.TrackSids.Add(publisher.Tracks[0].Sid);
            await client.UpdateSubscriptions(updateSubscriptionsRequest);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Send_Data()
        {
            await client.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            var sendDataRequest = new SendDataRequest
            {
                Room = TestConstants.ROOM_NAME,
                Data = ByteString.CopyFromUtf8("test-data"),
                Kind = DataPacket.Types.Kind.Reliable,
            };
            var response = await client.SendData(sendDataRequest);
            Assert.NotNull(response);
        }

        [Fact(Skip = "Only available in LiveKit Cloud")]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Forward_Participant()
        {
            // Create source room and join participant, publish track
            string sourceRoom = TestConstants.ROOM_NAME;
            string destRoom = TestConstants.ROOM_NAME + "-forward";
            string participantIdentity = TestConstants.PARTICIPANT_IDENTITY;

            await client.CreateRoom(new CreateRoomRequest { Name = sourceRoom });
            await fixture.PublishVideoTrackInRoom(client, sourceRoom, participantIdentity);

            // Get participant and track info in source room
            var participant = await client.GetParticipant(
                new RoomParticipantIdentity { Room = sourceRoom, Identity = participantIdentity }
            );
            Assert.NotNull(participant);
            Assert.Single(participant.Tracks);
            var track = participant.Tracks[0];

            // Create an empty destination room
            await client.CreateRoom(new CreateRoomRequest { Name = destRoom });

            // Forward participant to destination room
            var forwardRequest = new ForwardParticipantRequest
            {
                Room = sourceRoom,
                Identity = participantIdentity,
                DestinationRoom = destRoom,
            };
            await client.ForwardParticipant(forwardRequest);

            // Check participant and track in destination room
            var destParticipant = await client.GetParticipant(
                new RoomParticipantIdentity { Room = destRoom, Identity = participantIdentity }
            );
            Assert.NotNull(destParticipant);
            Assert.Equal(participantIdentity, destParticipant.Identity);
            Assert.Single(destParticipant.Tracks);
            Assert.Equal(track.Sid, destParticipant.Tracks[0].Sid);

            // Change participant and track in source room
            // Mute the track and update participant metadata
            await client.MutePublishedTrack(
                new MuteRoomTrackRequest
                {
                    Room = sourceRoom,
                    Identity = participantIdentity,
                    TrackSid = track.Sid,
                    Muted = true,
                }
            );
            await client.UpdateParticipant(
                new UpdateParticipantRequest
                {
                    Room = sourceRoom,
                    Identity = participantIdentity,
                    Metadata = "updated-metadata",
                }
            );

            // Check changes are reflected in destination room
            destParticipant = await client.GetParticipant(
                new RoomParticipantIdentity { Room = destRoom, Identity = participantIdentity }
            );
            Assert.True(destParticipant.Tracks[0].Muted);
            Assert.Equal("updated-metadata", destParticipant.Metadata);

            // Disconnect participant from source room
            await client.RemoveParticipant(
                new RoomParticipantIdentity { Room = sourceRoom, Identity = participantIdentity }
            );

            // Participant should be disconnected from destination room as well
            var destParticipants = await client.ListParticipants(
                new ListParticipantsRequest { Room = destRoom }
            );
            Assert.DoesNotContain(
                destParticipants.Participants,
                p => p.Identity == participantIdentity
            );
        }

        [Fact(Skip = "Only available in LiveKit Cloud")]
        [Trait("Category", "Integration")]
        [Trait("Category", "RoomService")]
        public async Task Move_Participant()
        {
            // Create source room and join participant, publish track
            string sourceRoom = TestConstants.ROOM_NAME;
            string destRoom = TestConstants.ROOM_NAME + "-move";
            string participantIdentity = TestConstants.PARTICIPANT_IDENTITY;

            await client.CreateRoom(new CreateRoomRequest { Name = sourceRoom });
            await fixture.PublishVideoTrackInRoom(client, sourceRoom, participantIdentity);

            // Get participant and track info in source room
            var participant = await client.GetParticipant(
                new RoomParticipantIdentity { Room = sourceRoom, Identity = participantIdentity }
            );
            Assert.NotNull(participant);
            Assert.Single(participant.Tracks);
            var track = participant.Tracks[0];

            // Create an empty destination room
            await client.CreateRoom(new CreateRoomRequest { Name = destRoom });

            // Move participant to destination room
            var moveRequest = new MoveParticipantRequest
            {
                Room = sourceRoom,
                Identity = participantIdentity,
                DestinationRoom = destRoom,
            };
            await client.MoveParticipant(moveRequest);

            // Check participant is no longer in source room
            var sourceParticipants = await client.ListParticipants(
                new ListParticipantsRequest { Room = sourceRoom }
            );
            Assert.DoesNotContain(
                sourceParticipants.Participants,
                p => p.Identity == participantIdentity
            );

            // Check participant and track in destination room
            var destParticipant = await client.GetParticipant(
                new RoomParticipantIdentity { Room = destRoom, Identity = participantIdentity }
            );
            Assert.NotNull(destParticipant);
            Assert.Equal(participantIdentity, destParticipant.Identity);
            Assert.Single(destParticipant.Tracks);
            Assert.Equal(track.Sid, destParticipant.Tracks[0].Sid);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        // After each test delete all rooms
        public async Task DisposeAsync()
        {
            var timeout = DateTime.Now.AddSeconds(60);
            var activeRooms = (await client.ListRooms(new ListRoomsRequest())).Rooms;
            while (activeRooms.Count > 0 && DateTime.Now < timeout)
            {
                foreach (var room in activeRooms)
                {
                    await client.DeleteRoom(new DeleteRoomRequest { Room = room.Name });
                }
                await Task.Delay(700);
                activeRooms = (await client.ListRooms(new ListRoomsRequest())).Rooms;
            }
            if (DateTime.Now >= timeout)
            {
                Assert.Fail("Timeout waiting for rooms to be deleted");
            }
        }
    }
}
