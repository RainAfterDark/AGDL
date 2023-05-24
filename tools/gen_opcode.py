import json

# path to packetIds.json
PACKET_IDS_FILE = ""

with open("../Common/Opcode.cs", "w") as opcodes:
    opcodes.write("namespace Common;\n\npublic enum Opcode {\n")
    with open(PACKET_IDS_FILE) as packetIds:
        for k, v in json.load(packetIds).items():
            opcodes.write(f"\t{v} = {k},\n")
    opcodes.write("}")
