from network import Server

if __name__ == "__main__":
    server = Server()
    server.initialize()

    server.loop()
