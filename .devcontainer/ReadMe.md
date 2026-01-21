# One-Time Rider + Podman  Setup

As far as I can tell, Rider does not honor the `:z` SELinux relabel option in `workspaceMount`. When inspecting the created container, the mount appears as `rprivate,rbind` without the `z` flag, causing Podman + SELinux to block access to the workspace (including the `.idea` directory).

You can work around this locally by running this once to set the SELinux context to `container_file_t`:
```bash
PROJECT_DIR="$HOME/RiderProjects"  # or wherever your devcontainer projects live
sudo semanage fcontext -a -t container_file_t "${PROJECT_DIR}(/.*)?"
sudo restorecon -Rv "$PROJECT_DIR"
```