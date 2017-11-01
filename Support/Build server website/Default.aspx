<%@ Page Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%

string root = Server.MapPath(".");

%>
<html>
<head>
  <title>Pdfium Build Server</title>
  <style>
    * {
      font-family: Tahoma, Arial, Sans-Serif;
    }
    body, table, p {
      font-size: 10pt;
    }
    h1 {
      font-size: 17pt;
    }
    td {
      vertical-align: top;
    }
    table { 
      border-spacing: 0;
      border-collapse: collapse;
      border: solid 2px silver;
    }
    td, th {
      padding: 4px;
      margin: 0;
      border: solid 1px silver;
    }
    .success {
      color: green;
    }
    .failed {
      color: red;
    }
  </style>
  <script src="https://code.jquery.com/jquery-3.2.1.min.js"></script>
</head>
<body>

  <h1>Pdfium Build Server</h1>
  
  <p>
    <a href="javascript:void(0)" onclick="toggleAllFailed();" id="toggle-all-failed">Show failed builds</a>
  </p>

  <table>
    <tr>
      <th>Date</th>
      <th>Build status</th>
      <th>Revision</th>
      <th>Build log</th>
    </tr>
    <% foreach (string date in Directory.GetDirectories(root).Select(p => Path.GetFileName(p)).OrderByDescending(p => p)) { %>
      <%
      
      int success = 0;
      int failed = 0;
      
      foreach (string directory in Directory.GetDirectories(Path.Combine(root, date))) {
        if (Directory.GetFiles(directory).Count() > 0) {
          success++;
        } else {
          failed++;
        }
      }
      
      %>
      <tr <% if (success == 0) { %> class="all-failed" style="display: none;" <% } %>>
        <td><%= date %></td>
        <td>
          <%
          
          if (success > 0) {
            %><span class="success"><%= success %> succeeded</span><%
          }
          if (success > 0 && failed > 0) {
            %> / <%
          }
          if (failed > 0) {
            %><span class="failed"><%= failed %> failed</span><%
          }
          
          %>
          <% if (success > 0) { %>
            <br/>
            <br/>
            <%
            
            foreach (string directory in Directory.GetDirectories(Path.Combine(root, date)).OrderBy(p => p)) {
              string fileName = Path.GetFileName(Directory.GetFiles(directory).SingleOrDefault());
              
              if (fileName != null) {
                %><a href="<%= date %>/<%= Path.GetFileName(directory) %>/<%= fileName %>"><%= Path.GetFileName(directory) %></a><br/><%
              }
            }
            
            %>
          <% } %>
        </td>
        <td>
          <%
          
          string buildlog = Path.Combine(root, date, "buildlog.txt");
          
          string revision = null;
          
          if (File.Exists(buildlog)) {
            foreach (string line in File.ReadLines(buildlog)) {
              var match = Regex.Match(line, "HEAD is now at ([a-fA-F0-9]+)");
              if (match.Success) {
                revision = match.Groups[1].Value;
                break;
              }
            }
          }
          
          if (revision != null) {
            %><a href="https://pdfium.googlesource.com/pdfium/+/<%= revision %>"><%= revision %></a><%
          } else {
            %>&nbsp;<%
          }
          
          %>
        </td>
        <td>
          <% if (File.Exists(buildlog)) { %>
          <a href="<%= date %>/buildlog.txt">buildlog.txt</a>
          <% } else { %>
          buildlog.txt
          <% } %>
        </td>
      </tr>
    <% } %>
  </table>
  
  <script>
    var showingAllFailed = false;
    
    function toggleAllFailed() {
      if (showingAllFailed) {
        $('#toggle-all-failed').text('Show failed builds');
        $('.all-failed').hide();
      } else {
        $('#toggle-all-failed').text('Hide failed builds');
        $('.all-failed').show();
      }
      
      showingAllFailed = !showingAllFailed;
    }
    
  </script>
  
</body>
</html>
